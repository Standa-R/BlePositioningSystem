#include <stdbool.h>
#include "string.h"
#include "BLE.h"
#include "ZigBee.h"
#include "userConfig.h"

#include "main.h"

#define BUFFER_SIZE 256  /* must be power of two */
#define COMMAND_BUFFER_SIZE 128

#define NUM_OF_MODULES 4

#define QUEUE_SIZE 60

typedef enum {DecodeID, DecodeRSSI, DecodeChannel} DecodePart;
typedef enum { Init = 0, ListeningAdv, Reset} BleState;


typedef struct
{
    uint8_t    rxBuffer[BUFFER_SIZE];
    uint16_t   lastProccessed;
    uint8_t    commandBuffer[COMMAND_BUFFER_SIZE];
    uint8_t    commandBufferIndex;
}COMM_MODULE;


typedef enum {Module1 = 0, Module2, Module3, Module4} Modules;

static COMM_MODULE mods[NUM_OF_MODULES];



static QueueItem queue[QUEUE_SIZE];
static uint8_t queueBottom;
static uint8_t queueTop;

static bool GetDataFromReport(uint8_t *inputString, uint8_t lenght, REPORT *decodedReport);

UART_HandleTypeDef* mod0;
UART_HandleTypeDef* mod1;
UART_HandleTypeDef* mod2;
UART_HandleTypeDef* mod3;

static BleState bleState;
static PositioningMode positioingMode = PositioningModeRemote;

static const uint8_t startCmdListen[] = "\rat+run \"adscan\"\r";        //run as listening anchors
static const uint8_t startCmdTrasmit[] = "\rat+run \"sAnchor\"\r";        //run as trasmitting anchors
static uint8_t anchorAntenna[] = "iA0A0\r";

static uint8_t *startCmd;

static bool filterEnable;
static uint64_t advAddr[255];
static uint16_t advAddrIndex;

static uint32_t tickReference;

static bool GetDataFromReport(uint8_t *inputString, uint8_t lenght, REPORT *decodedReport);
static void ProcessReport(REPORT *report, Modules module);
static void CheckForCompleteRecord(QueueItem* queue, uint16_t item);

static bool resetModules;

#define ProcessModuleMessage(module, hdma) \
      if(mods[module].lastProccessed != hdma->Instance->CNDTR) \
      { \
          char tmpChar = mods[module].rxBuffer[BUFFER_SIZE - mods[module].lastProccessed--]; \
          if(mods[module].lastProccessed == 0) \
          { \
              mods[module].lastProccessed = BUFFER_SIZE; \
          } \
          mods[module].commandBuffer[mods[module].commandBufferIndex++] = tmpChar; \
          if(tmpChar == '\r') \
          { \
              REPORT reportMod; \
              if (GetDataFromReport(mods[module].commandBuffer, mods[module].commandBufferIndex, &reportMod)) \
              { \
                  ProcessReport(&reportMod, module); \
              }\
              mods[module].commandBufferIndex = 0; \
          }else{ \
               \
          } \
      }

void ProcessModuleMessage2(Modules module, DMA_HandleTypeDef *hdma)
{
    uint32_t dmaNDTR;
    __disable_irq();                      //requred for atomic operation
    dmaNDTR = hdma->Instance->CNDTR;
    __enable_irq();
    
    if(mods[module].lastProccessed != dmaNDTR)
      {
          char tmpChar = mods[module].rxBuffer[BUFFER_SIZE - mods[module].lastProccessed--];
          if(mods[module].lastProccessed == 0)
          {
              mods[module].lastProccessed = BUFFER_SIZE;
          }
          mods[module].commandBuffer[mods[module].commandBufferIndex++] = tmpChar;
          if(tmpChar == '\r')
          {
              REPORT reportMod;
              if (GetDataFromReport(mods[module].commandBuffer, mods[module].commandBufferIndex, &reportMod))
              {
                  ProcessReport(&reportMod, module);
              }
              mods[module].commandBufferIndex = 0;
          }else{
               
          }
      }
    
}

void BleInit(UART_HandleTypeDef* _mod0, UART_HandleTypeDef* _mod1, UART_HandleTypeDef* _mod2, UART_HandleTypeDef* _mod3)
{
    mod0 = _mod0;
    mod1 = _mod1;
    mod2 = _mod2;
    mod3 = _mod3;
    
    __HAL_UART_FLUSH_DRREGISTER(mod0);     //discard any bytes in register which could be generated by power up routins of modules
    __HAL_UART_FLUSH_DRREGISTER(mod1);
    __HAL_UART_FLUSH_DRREGISTER(mod2);
    __HAL_UART_FLUSH_DRREGISTER(mod3);
    
    HAL_UART_Receive_DMA(mod0, mods[Module1].rxBuffer, BUFFER_SIZE);       //start receive DMA channels, RX chars are stored in ring buffer
    HAL_UART_Receive_DMA(mod1, mods[Module2].rxBuffer, BUFFER_SIZE);
    HAL_UART_Receive_DMA(mod2, mods[Module3].rxBuffer, BUFFER_SIZE);
    HAL_UART_Receive_DMA(mod3, mods[Module4].rxBuffer, BUFFER_SIZE);
    
    mods[Module1].lastProccessed = BUFFER_SIZE;
    mods[Module2].lastProccessed = BUFFER_SIZE;
    mods[Module3].lastProccessed = BUFFER_SIZE;
    mods[Module4].lastProccessed = BUFFER_SIZE;
    
    uint8_t i;
    for(i = 0; i < QUEUE_SIZE; i++)
    {
      queue[i].NumOfRecords = -1;
      queue[i].rssi[0] = -110;
      queue[i].rssi[1] = -110;
      queue[i].rssi[2] = -110;
      queue[i].rssi[3] = -110;

      queue[i].channel[0] = 0;
      queue[i].channel[1] = 0;
      queue[i].channel[2] = 0;
      queue[i].channel[3] = 0;
      
    }
    
    //startCmd = (uint8_t*)startCmdListen;
    bleState = Init;
    
}



void BleTask()
{
    switch(bleState)
    {
        case Reset:
            ZigBeeReset();
            HAL_Delay(100);
            HAL_GPIO_WritePin(Reset_GPIO_Port, Reset_Pin, GPIO_PIN_RESET);
            HAL_Delay(200);
            HAL_GPIO_WritePin(Reset_GPIO_Port, Reset_Pin, GPIO_PIN_SET);
            ZigBeeStartInit();
            HAL_Delay(500);
        
            bleState = Init;
            break;
        
        case Init:
            __HAL_UART_FLUSH_DRREGISTER(mod0);     //discard any bytes in register which could be generated by power up routins of modules
            __HAL_UART_FLUSH_DRREGISTER(mod1);
            __HAL_UART_FLUSH_DRREGISTER(mod2);
            __HAL_UART_FLUSH_DRREGISTER(mod3);
            
            HAL_UART_Receive_DMA(mod0, mods[Module1].rxBuffer, BUFFER_SIZE);       //start receive DMA channels, RX chars are stored in ring buffer
            HAL_UART_Receive_DMA(mod1, mods[Module2].rxBuffer, BUFFER_SIZE);
            HAL_UART_Receive_DMA(mod2, mods[Module3].rxBuffer, BUFFER_SIZE);
            HAL_UART_Receive_DMA(mod3, mods[Module4].rxBuffer, BUFFER_SIZE);
        
            switch(positioingMode)
            {
                case PositioningModeRemote:
                    HAL_UART_Transmit_DMA(mod0, (uint8_t*)startCmdListen, strlen((const char*)startCmdListen));
                    HAL_Delay(132);
                    HAL_UART_Transmit_DMA(mod1, (uint8_t*)startCmdListen, strlen((const char*)startCmdListen));
                    HAL_Delay(132);
                    HAL_UART_Transmit_DMA(mod2, (uint8_t*)startCmdListen, strlen((const char*)startCmdListen));
                    HAL_Delay(132);
                    HAL_UART_Transmit_DMA(mod3, (uint8_t*)startCmdListen, strlen((const char*)startCmdListen));
                    HAL_Delay(132);
                    break;
                
                case PositioningModeSelf:
                    HAL_Delay(20*ConfigAnchorId);
                    HAL_UART_Transmit_DMA(mod0, (uint8_t*)startCmdTrasmit, strlen((const char*)startCmdTrasmit));
                    HAL_Delay(8);
                    HAL_UART_Transmit_DMA(mod1, (uint8_t*)startCmdTrasmit, strlen((const char*)startCmdTrasmit));
                    HAL_Delay(8);
                    HAL_UART_Transmit_DMA(mod2, (uint8_t*)startCmdTrasmit, strlen((const char*)startCmdTrasmit));
                    HAL_Delay(8);
                    HAL_UART_Transmit_DMA(mod3, (uint8_t*)startCmdTrasmit, strlen((const char*)startCmdTrasmit));
                    HAL_Delay(8);
                
                    
                    anchorAntenna[2] = ConfigAnchorId + '0';
                    anchorAntenna[4] = '0';
                    HAL_UART_Transmit_DMA(mod0, anchorAntenna, 6);
                    HAL_Delay(4);
                    anchorAntenna[4] = '1';
                    HAL_UART_Transmit_DMA(mod1, anchorAntenna, 6);
                    HAL_Delay(4);    
                    anchorAntenna[4] = '2';
                    HAL_UART_Transmit_DMA(mod2, anchorAntenna, 6);
                    HAL_Delay(4);
                    anchorAntenna[4] = '3';
                    HAL_UART_Transmit_DMA(mod3, anchorAntenna, 6);
                       
                    
                    break;
                    
            }
            bleState = ListeningAdv;
            break;
        
        case ListeningAdv:
        {
            /*=== Process receive ring buffers of BLE modules ===*/
//            ProcessModuleMessage(Module1, hdma_usart3_rx);
//            ProcessModuleMessage(Module2, hdma_usart1_rx);
//            ProcessModuleMessage(Module3, hdma_usart4_rx);
//            ProcessModuleMessage(Module4, hdma_usart5_rx);

            
            ProcessModuleMessage2(Module1, mod0->hdmarx);
            ProcessModuleMessage2(Module2, mod1->hdmarx);
            ProcessModuleMessage2(Module3, mod2->hdmarx);
            ProcessModuleMessage2(Module4, mod3->hdmarx);

            uint8_t i;
            if(queueBottom < queueTop)
            {  
                for(i = queueBottom; i < queueTop; i++)
                {
                  CheckForCompleteRecord(queue, i);
                }
            }
            else // queue rolled over, now bottom is greater than top
            {
                for(i = queueBottom; i < QUEUE_SIZE; i++)
                {
                  CheckForCompleteRecord(queue, i);
                }

                for(i = 0; i < queueTop; i++)
                {
                  CheckForCompleteRecord(queue, i);
                }
            }
        
        }
        break;
        
        
        default:
            break;
    }
    
    if(resetModules)
    {
        resetModules = false;
        bleState = Reset;
    }
}


static bool GetDataFromReport(uint8_t *inputString, uint8_t lenght, REPORT *decodedReport)
{
    DecodePart decodePart;
    uint8_t c;
    
    uint64_t localId = 0;
    int8_t localRssi = 0;
    uint8_t localChannel = 0;
    
    int8_t applySign = 1;
    
    if(inputString[0] == ';')
    {
        uint8_t i;   
        decodePart = DecodeID;
        
        for(i = 1; i < lenght; i++)
        {
            c = inputString[i]; 
            if((c == ',') || (c == '\r'))
            {
                switch(decodePart)
                {
                    case DecodeID:
                        decodedReport->AdvAddr = localId;
                        decodePart = DecodeRSSI;
                        break;
                    
                    case DecodeRSSI:
                        decodedReport->rssi = applySign * localRssi;
                        decodePart = DecodeChannel;
                        break;
                    
                    case DecodeChannel:
                        decodedReport->channel = localChannel;
                        return true;
                }
            }
            else
            {
                switch(decodePart)
                {
                    case DecodeID:                
                        localId <<= 4;
                        if ((c >= '0') && (c <= '9'))            
                        {
                            localId |= (c - '0');
                        }
                        else if ((c >= 'A') && (c <= 'F'))
                        {
                            localId  |= (10 + c - 'A');
                        }
                        else
                        {
                            return false;
                        }
                        break;
                        
                    case DecodeRSSI:
                        localRssi *= 10;
                        if ((c >= '0') && (c <= '9'))            
                        {
                            localRssi += (c - '0');
                        }
                        else if (c == '-')
                        {
                            applySign = -1;
                        }else{
                            return false;
                        }
                        break;
                        
                    case DecodeChannel:
                        if ((c >= '0') && (c <= '9'))            
                        {
                            localChannel = (c - '0');
                        }
                        else{
                            return false;
                        }
                        break;
                }
            }    
        }
    }
    return false;
}

static void ProcessReport(REPORT *report, Modules module)
{
    QueueItem *qi;
    
    if(filterEnable == true)
    {
        if (report->AdvAddr != advAddr[0])
        {
            return;
        }
    }    
    
    qi = FindItem(report->AdvAddr);
    if(qi != NULL)
    {
      qi->channel[module] = report->channel;
      qi->rssi[module] = report->rssi;
      qi->NumOfRecords++;
    }
}

QueueItem* FindItem(uint64_t advAddr)
{
    uint8_t itemsInQueue;
    uint8_t i;
    
    if(queueBottom != queueTop)
    {
        if (queueBottom < queueTop)         //all ok bottom is under top
        {
            itemsInQueue = queueTop - queueBottom;
            for(i=queueBottom;i<queueTop;i++) //seach for AdvAddr in queue
            {               
                if(advAddr == queue[i].AdvAddr)  //id found, return queue item
                {
                    return &queue[i];
                }
            }
        }
        else
        {                              //there has been a overflow from top addres to 0, does not mean the queue is full
            itemsInQueue = QUEUE_SIZE - queueBottom + queueTop;
            
            for(i=queueBottom;i<QUEUE_SIZE;i++) //seach for AdvAddr in queue, bottom to top slots
            {
                if(advAddr == queue[i].AdvAddr)  //id found, return queue item
                {
                    return &queue[i];
                }
            }
            
            for(i=0;i<queueTop;i++) //seach for AdvAddr in queue, form 0 to top slots
            {
                if(advAddr ==queue[i].AdvAddr)  //id found, return queue item
                {
                    return &queue[i];
                }
            }
        }
    }
    return GetFreeQueueItem(advAddr);    //queue is empty or AdvAddr was not found create a new entry for AdvAddr and return the queue slot
}

///Gets a spot in ReportQueue, fills created time (reports from four antennnas need to be received in 4 miliseconds to ensure that they comes from a single advertisement, 
///                            in case there is received less than 4 reports after 4 miliseconds only 3 reports are sent to system)
///                          , and fills AdvAddress (id)
///
QueueItem* GetFreeQueueItem(uint64_t id)
{
    uint8_t tmpQTop;
    if (queueBottom != queueTop)
    {
        if(queueBottom < queueTop)
        {
            if((queueTop - queueBottom) >= (QUEUE_SIZE - 1) )
            {
                return NULL;
            }
        }
        else
        {
            if( (QUEUE_SIZE - queueBottom + queueTop) >= (QUEUE_SIZE - 1) )
            {
                return NULL;
            }
        }
    }
    queue[queueTop].CreateTime = HAL_GetTick();
    queue[queueTop].AdvAddr = id;
    queue[queueTop].NumOfRecords = 0;
    
    tmpQTop = queueTop;
    queueTop++;
    if (queueTop >= QUEUE_SIZE)
    {
        queueTop = 0;
    }
    return &queue[tmpQTop];
}

static void CheckForCompleteRecord(QueueItem* queue, uint16_t item)
{
    QueueItem *queueItem = &queue[item];
    
    
    
    if(queueItem->NumOfRecords != -1)
    {
        if (((queueItem->CreateTime + 5) < HAL_GetTick()) || (queueItem->NumOfRecords == 4))    //send record if the timeout has elapsaed or all four records has been received
        {                      
            //TransmitData(queue[i].rawData, 20);
            queueItem->CreateTime -= tickReference;
            ZigBeeQueueNewReport(queueItem->rawData);
            queueItem->NumOfRecords = -1;
            queueItem->rssi[0] = -110;
            queueItem->rssi[1] = -110;
            queueItem->rssi[2] = -110;
            queueItem->rssi[3] = -110;
            
            queueItem->channel[0] = 0;
            queueItem->channel[1] = 0;
            queueItem->channel[2] = 0;
            queueItem->channel[3] = 0;
            if(item == queueBottom)
            {
                queueBottom++;
                if (queueBottom >= QUEUE_SIZE)
                {
                    queueBottom = 0;
                }
            }
        }
    }
    else
    {
        if (item == queueBottom)
        {
          queueBottom++;
          if (queueBottom >= QUEUE_SIZE)
          {
              queueBottom = 0;
          }
        }
    }
}



void BleAddressFilterEnable()
{
    filterEnable = true;
}

void BleAddressFilterDisable()
{
    filterEnable = false;
}

void BleAddressFilterAdd(uint64_t address)
{
    advAddr[advAddrIndex] = address;
}

void BleResetTicks()
{
    tickReference = HAL_GetTick();
}

void PositioningModeSet(PositioningMode _positioingMode)
{
    if(positioingMode != _positioingMode)
    {
        positioingMode = _positioingMode;
        resetModules = true;
    }
//    switch(positioingMode)
//    {
//        case PositioningModeRemote:
//            
//            break;
//        
//        case PositioningModeSelf:
//            
//            break;
//    }
}

PositioningMode PositioningModeGet()
{
    return positioingMode;
}
