#include "stm32f4xx_hal.h"


void ZigBeeInit(UART_HandleTypeDef* mod);
void ZigBeeTask(void);

void ZigBeeQueueNewReport(uint8_t *msg);
