#include "stm32f0xx_hal.h"


void ZigBeeInit(UART_HandleTypeDef* mod);
void ZigBeeTask(void);

void ZigBeeQueueNewReport(uint8_t *msg);

void ZigBeeReset(void);
void ZigBeeStartInit(void);


void ZigBeeReinitializeDMA(void);