#include <string.h>
#include <stdbool.h>
#include "ZigBee.h"
#include "BLE.h"
#include "main.h"
#include "ServiceUartIF.h"

typedef enum { Init = 0, WaitForLaunch, Operation, Reset} ZigBeeState;

#define BUFFER_SIZE 256  /* must be power of two */
#define COMMAND_BUFFER_SIZE 128

static ZigBeeState zigBeeState;

typedef struct
{
    uint8_t    rxBuffer[BUFFER_SIZE];
    uint16_t   lastProccessed;
    uint8_t    commandBuffer[COMMAND_BUFFER_SIZE];
    uint8_t    commandBufferIndex;
}COMM_MODULE;


#define TX_QUEUE_ZIGBEE_SIZE 21
static uint8_t txQueueZigBee[TX_QUEUE_ZIGBEE_SIZE][40];     //the length of message is stored in 1st byte!!!
static uint16_t txQueueZigBeeStart;
static uint16_t txQueueZigBeeEnd;

static bool transmitStarted;
static bool zigBeeReady;

static bool zigBeeReset;
static bool zigBeeStartInit;

static uint8_t startTransmit[] = "AT+UCASTB:19,000D6F000E286A5D\r";
static uint8_t startTransmitShort[] = "AT+UCASTB:05,000D6F000E286A5D\r";

unsigned char messageStart[] = {'\r', '\n', 'B', 'C', 'A', 'S', 'T', ':'};
int messageStartLength = sizeof(messageStart);

typedef enum {DecodingSearch, DecodingTriggered, DecodingReceiveEUI, DecodingMessageLen, DecodingMessage} DecodingState;
static DecodingState sm;
static uint16_t searchingIndex = 0;
static uint16_t messageLength;
static uint64_t eui;

static COMM_MODULE modZigBee;

static UART_HandleTypeDef* zigBeeHandle;


void ProcessZigBeeMessage(void);



void ZigBeeInit(UART_HandleTypeDef* _zigBeeHandle)
{
    zigBeeHandle = _zigBeeHandle;
    __HAL_UART_FLUSH_DRREGISTER(zigBeeHandle);     //discard any bytes in register which could be generated by power up routins of modules
    
    modZigBee.lastProccessed = BUFFER_SIZE;
    HAL_UART_Receive_DMA(zigBeeHandle, modZigBee.rxBuffer, BUFFER_SIZE);
    
    zigBeeState = Init;
}

static uint32_t delayAux;
static uint32_t delayIdleStatusReport;

static uint8_t setSourceRoute[] = "AT+SR:0000\r";


static bool resetDbg;

void ZigBeeTask()
{
    switch(zigBeeState)
    {
        case Reset:
            if(zigBeeStartInit)
            {
                zigBeeStartInit = false;
                zigBeeState = Init;
                resetDbg = true;
            }
            break;
        
        case Init:
            delayAux = HAL_GetTick();
            zigBeeState = WaitForLaunch;
            break;
        
        case WaitForLaunch:
            if (HAL_GetTick() >= delayAux + 500)
            {
                __HAL_UART_FLUSH_DRREGISTER(zigBeeHandle);
                HAL_UART_Receive_DMA(zigBeeHandle, modZigBee.rxBuffer, BUFFER_SIZE);
                
                HAL_UART_Transmit_DMA(zigBeeHandle, setSourceRoute, strlen((char*)setSourceRoute));
                //HAL_Delay(10);
                delayIdleStatusReport = HAL_GetTick();
                zigBeeState = Operation;
            }
            break;
        
        case Operation:
            if(transmitStarted == false)
            {
                if(txQueueZigBeeStart != txQueueZigBeeEnd)
                {
                    if(zigBeeHandle->gState == HAL_UART_STATE_READY)
                    {
                        if (txQueueZigBee[txQueueZigBeeStart][0] <= 5)
                        {
                            HAL_UART_Transmit_DMA(zigBeeHandle, startTransmitShort, 30);
                        }
                        else
                        {
                            HAL_UART_Transmit_DMA(zigBeeHandle, startTransmit, 30);
                        }
                        
                        transmitStarted = true;
                    }
                }
            }

            if(zigBeeReady)
            {
                if(zigBeeHandle->gState == HAL_UART_STATE_READY)
                {
                    HAL_UART_Transmit_DMA(zigBeeHandle, &txQueueZigBee[txQueueZigBeeStart][1], txQueueZigBee[txQueueZigBeeStart][0]);
                    txQueueZigBeeStart++;
                    if(txQueueZigBeeStart >= TX_QUEUE_ZIGBEE_SIZE)
                    {
                        txQueueZigBeeStart = 0;
                    }
                    zigBeeReady = false;
                    transmitStarted = false;
                    delayIdleStatusReport = HAL_GetTick();
                }   
            }
            
            if(HAL_GetTick() >= delayIdleStatusReport + 2000)
            {
                delayIdleStatusReport = HAL_GetTick();
                //if (resetDbg)
                {
                    ZigBeeQueueNewReport(0);        //zero is for status update
                }
            }
            
            
            //while(modZigBee.lastProccessed != hdma_usart2_rx.Instance->CNDTR)
            uint32_t dmaNDTR;
            __disable_irq();                        //requred for atomic operation
            dmaNDTR = zigBeeHandle->hdmarx->Instance->CNDTR;
            __enable_irq();
            while(modZigBee.lastProccessed != dmaNDTR)
            {
                char c = modZigBee.rxBuffer[BUFFER_SIZE - modZigBee.lastProccessed--];
                if(modZigBee.lastProccessed == 0)
                {
                  modZigBee.lastProccessed = BUFFER_SIZE;
                }
                
                switch(sm)
                {
                    case DecodingTriggered:
                        if (c == messageStart[searchingIndex++])
                        {
                            if(searchingIndex == messageStartLength)
                            {
                                searchingIndex = 0;
                                eui = 0;
                                sm = DecodingReceiveEUI;
                                break;
                            }
                        }
                        else
                        {
                            sm = DecodingSearch;
                        }
                        //no break intended
                        
                    case DecodingSearch:
                        if (c == messageStart[0])
                        {
                            sm = DecodingTriggered;
                            searchingIndex = 1;
                        }
                        break;
                        
                    case DecodingReceiveEUI:
                        if (searchingIndex < 16)
                        {
                            eui <<= 4;
                            if ((c >= '0') && (c <= '9'))            
                            {
                                eui |= (c - '0');
                            }
                            else if ((c >= 'A') && (c <= 'F'))
                            {
                                eui  |= (10 + c - 'A');
                            }
                            searchingIndex++;
                        }
                        else
                        {
                            if (c == ',')
                            {
                               searchingIndex = 0;
                               messageLength = 0;
                               sm = DecodingMessageLen;
                            }
                            else
                            {
                                sm = DecodingSearch;
                            }
                        }
                        break;
                        
                    case DecodingMessageLen:
                        if(searchingIndex < 2)
                        {
                            messageLength <<= 4;
                            if ((c >= '0') && (c <= '9'))            
                            {
                                messageLength |= (c - '0');
                            }
                            else if ((c >= 'A') && (c <= 'F'))
                            {
                                messageLength  |= (10 + c - 'A');
                            }
                            searchingIndex++;
                        }
                        else
                        {
                            if (c == '=')
                            {
                                searchingIndex = 0;
                                sm = DecodingMessage;
                            }else{
                                sm = DecodingSearch;
                            }
                        }
                        break;
                        
                    case DecodingMessage:
                        {
                            if(searchingIndex >= 0)
                            {
                                //rx
                                modZigBee.commandBuffer[searchingIndex] = c;
                                
                                if (searchingIndex == (messageLength - 1))
                                {
                                    ProcessZigBeeMessage();
                                    //item.Eui = eui;
                                    //memcpy(&UdpMessage[4], &item.rawData[0], 28);
                                    //UdpSend(UdpMessage, 32);
                                    //cout << std::fixed << std::setprecision(3) << (float)item.CreateTime / 1000.0 << " s => id: " << hex << item.AdvAddr << ", Rssi1:" << dec << (int)item.rssi[0] << ", Rssi2:" << dec << (int)item.rssi[1] << ", Rssi3:" << dec << (int)item.rssi[2] << ", Rssi4:" << dec << (int)item.rssi[3] << "\n" << flush;
                                    sm = DecodingSearch;
                                }
                            }
                            searchingIndex++;
                        }
                        break;
                }
                
                if (sm == DecodingSearch) 
                {
                    if(transmitStarted)
                    {
                      if(c == '>')
                      {
                          HAL_GPIO_TogglePin(LED_GPIO_Port, LED_Pin);
                          zigBeeReady = true;
                      }
                    }
                }
            }
            
            
            break;
        
        
        default:
            break;
    }
    
    if(zigBeeReset && !zigBeeReady && !transmitStarted)
    {
        zigBeeReset = false;
        zigBeeState = Reset;
    }
}

/// Queues message to be sent over ZigBee interface
void ZigBeeQueueNewReport(uint8_t *msg)
{    
    txQueueZigBee[txQueueZigBeeEnd][1] = 'B';
    txQueueZigBee[txQueueZigBeeEnd][2] = 'L';
    txQueueZigBee[txQueueZigBeeEnd][3] = 'L';
    txQueueZigBee[txQueueZigBeeEnd][4] = '\n';
    txQueueZigBee[txQueueZigBeeEnd][5] = PositioningModeGet();
    if(msg != 0)
    {    
        memcpy(&txQueueZigBee[txQueueZigBeeEnd][6], msg, 20);
        txQueueZigBee[txQueueZigBeeEnd][0] = 25;
    }
    else
    {
        txQueueZigBee[txQueueZigBeeEnd][0] = 5;
    }
    txQueueZigBeeEnd++;
    if (txQueueZigBeeEnd >= TX_QUEUE_ZIGBEE_SIZE)
    {
        txQueueZigBeeEnd = 0;
    }
    
    HAL_GPIO_TogglePin(LED_GPIO_Port, LED_Pin);
    HAL_GPIO_TogglePin(LED_GPIO_Port, LED_Pin);
}

#define head(h, t...) h
#define tail(h, t...) t

#define A(n, c...) (((long long) (head(c))) << (n)) | B(n + 8, tail(c))
#define B(n, c...) (((long long) (head(c))) << (n)) | C(n + 8, tail(c))
#define C(n, c...) (((long long) (head(c))) << (n)) | D(n + 8, tail(c))
#define D(n, c...) (((long long) (head(c))) << (n))
    
#define ProtocolMsg(c...) A(0, c, 0, 0, 0, 0, 0, 0, 0)

volatile int xy1;
volatile int xy2;

void ProcessZigBeeMessage(void)
{
    uint8_t command[4];
    
    xy1 = ProtocolMsg('\r', '\r');
    xy2 = 0x0d0d;
    
    command[0] = modZigBee.commandBuffer[0];
    command[1] = modZigBee.commandBuffer[1];
    command[2] = modZigBee.commandBuffer[2];
    command[3] = modZigBee.commandBuffer[3];
    
    switch(*((int*)command))
    {
        case 0x0A454642:  //BFE\n - bluetooth filter enable
            ServiceUartIFPrint("FilterEnable\n");
            BleAddressFilterEnable();
            break;
        
        case 0x0A444642:  //BFD\n - bluetooth flter disable
            ServiceUartIFPrint("FilterDisable\n");
            BleAddressFilterDisable();
            break;
        case 0x0A414642:  //BFA\n - bluetooth filter add
            ServiceUartIFPrint("Filter add\n");
            BleAddressFilterAdd((uint64_t)modZigBee.commandBuffer[4] | ((uint64_t)modZigBee.commandBuffer[5] << 8) |  ((uint64_t)modZigBee.commandBuffer[6] << 16) |  ((uint64_t)modZigBee.commandBuffer[7] << 24) | ((uint64_t)modZigBee.commandBuffer[8] << 32) | ((uint64_t)modZigBee.commandBuffer[9] << 40) | ((uint64_t)modZigBee.commandBuffer[10] << 48) | ((uint64_t)modZigBee.commandBuffer[11] << 56));
            break;
        case 0x0A525442:  //BTR\n - bluetooth time reset
            BleResetTicks();
            ServiceUartIFPrint("ResetTime\n");
            //tickReference = HAL_GetTick();
            break;
        case ProtocolMsg('B', 'R', 'P', '\n'):  //BRP\n - bluetooth remote positioning, anchors are listening
            ServiceUartIFPrint("remote\n");
            PositioningModeSet(PositioningModeRemote);
            break;
        
        case ProtocolMsg('B', 'S', 'P', '\n'):  //BSP\n - bluetooth self positioning, anchors are transmitting
            ServiceUartIFPrint("self\n");
            PositioningModeSet(PositioningModeSelf);
            break;
        
    }
}


void ZigBeeReset(void)
{
    zigBeeReset = true;
}

void ZigBeeStartInit(void)
{
    zigBeeStartInit = true;
}

void ZigBeeReinitializeDMA(void)
{
    __HAL_UART_FLUSH_DRREGISTER(zigBeeHandle);
    HAL_UART_Receive_DMA(zigBeeHandle, modZigBee.rxBuffer, BUFFER_SIZE);
    transmitStarted = false;
}