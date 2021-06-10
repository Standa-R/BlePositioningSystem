#include <string.h>
#include <stdio.h>
#include <stdarg.h>
#include "ServiceUartIF.h"

#define BUFFER_SIZE 256  /* must be power of two */
#define TX_BUFFER_SIZE 1024

static uint8_t rxBufferDBG[BUFFER_SIZE];
static uint8_t txBufferDBG[TX_BUFFER_SIZE];

static uint16_t txBufferDBGGStart;
static uint16_t txBufferDBGEnd;

static UART_HandleTypeDef* huart;


static uint16_t lastProccessed;
static uint8_t commandBuffer[20];
static uint8_t commandBufferIndex;

void ServiceUartIFInit(UART_HandleTypeDef* _huart)
{
    huart = _huart;
    lastProccessed = BUFFER_SIZE;
    HAL_UART_Receive_DMA(huart, rxBufferDBG, BUFFER_SIZE);
}



void ServiceUartIFTask()
{
    //check for buffer, if there are some new bytes send them
    if(txBufferDBGGStart != txBufferDBGEnd)
    {
        if(huart->gState == HAL_UART_STATE_READY)
        {
            if(txBufferDBGEnd > txBufferDBGGStart)
            {
                HAL_UART_Transmit_DMA(huart, &txBufferDBG[txBufferDBGGStart], txBufferDBGEnd - txBufferDBGGStart);
                txBufferDBGGStart = txBufferDBGEnd;                
            }
            else
            {
                //(TX_BUFFER_SIZE - txBufferDBGGStart) + 0 + txBufferDBGEnd
                HAL_UART_Transmit_DMA(huart, &txBufferDBG[txBufferDBGGStart], TX_BUFFER_SIZE - txBufferDBGGStart);
                txBufferDBGGStart = 0;
            }
        }
    }
    
    //check receive buffer
    if(lastProccessed != huart->hdmarx->Instance->CNDTR)
    {          
      char tmpChar = rxBufferDBG[BUFFER_SIZE - lastProccessed--];
      if(lastProccessed == 0)
      {
          lastProccessed = BUFFER_SIZE;
      }
      commandBuffer[commandBufferIndex++] = tmpChar;
      
      if(tmpChar == '\r')
      {
          HAL_UART_Transmit_DMA(huart, commandBuffer, commandBufferIndex);
          //HAL_UART_Transmit_DMA(&huart2, commandBuffer, commandBufferIndex);    //send to zigbee???
          commandBufferIndex = 0;
      }
    }
    
}

bool ServiceUartIFPrint(const char *str, ...)
{
    char printBuffer[200];
    va_list args;
    
    va_start(args, str);
    int32_t len = vsprintf(printBuffer, str, args);
    va_end(args);
    ServiceUartIFTransmitData((uint8_t*)printBuffer, len);
    return true;
}


void ServiceUartIFTransmitData(uint8_t *data, uint16_t dataLen)
{
//    if(hdma_usart6_tx.State == HAL_DMA_STATE_READY)
//    {
//        HAL_UART_Transmit_DMA(&huart6, data, dataLen);
//    }
//    else
    {
        if ((txBufferDBGEnd + dataLen) < TX_BUFFER_SIZE)
        {
//            memcpy(&txBufferDBG[txBufferDBGEnd], "BLL\n", 4);
//            txBufferDBGEnd += 4;
            memcpy(&txBufferDBG[txBufferDBGEnd], data, dataLen);
            txBufferDBGEnd += dataLen;
        }
        else
        {
            uint16_t numOfBytesToEnd = TX_BUFFER_SIZE - txBufferDBGEnd;
            uint16_t numOfBytesFromStart = dataLen - numOfBytesToEnd;
            
            memcpy(&txBufferDBG[txBufferDBGEnd], data, numOfBytesToEnd);
            memcpy(&txBufferDBG[0], &data[numOfBytesToEnd], numOfBytesFromStart);
            txBufferDBGEnd = numOfBytesFromStart;
        }
    }
}
