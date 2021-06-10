#include <stdbool.h>
#include "stm32f4xx_hal.h"


void ServiceUartIFInit(UART_HandleTypeDef* _huart);
void ServiceUartIFTask(void);
void ServiceUartIFTransmitData(uint8_t *data, uint16_t dataLen);
bool ServiceUartIFPrint(const char *str, ...);
