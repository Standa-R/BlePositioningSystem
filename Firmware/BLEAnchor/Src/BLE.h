#include "stm32f0xx_hal.h"
#pragma anon_unions

typedef enum {PositioningModeRemote = 0, PositioningModeSelf} PositioningMode;

typedef struct
{
    uint64_t AdvAddr;
    uint8_t channel;
    uint8_t rssi;
}REPORT;

typedef union
{
    struct
    {
        uint64_t AdvAddr;
        uint32_t CreateTime;
        uint8_t channel[4];
        int8_t rssi[4];
        int8_t NumOfRecords;
    };
    uint8_t rawData[20];
}QueueItem;


extern void BleInit(UART_HandleTypeDef* _mod0, UART_HandleTypeDef* _mod1, UART_HandleTypeDef* _mod2, UART_HandleTypeDef* _mod3);
extern void BleTask(void);

extern void BleAddressFilterEnable(void);
extern void BleAddressFilterDisable(void);
extern void BleAddressFilterAdd(uint64_t address);
extern void BleResetTicks(void);

extern QueueItem* FindItem(uint64_t advAddr);
extern QueueItem* GetFreeQueueItem(uint64_t id);

void PositioningModeSet(PositioningMode _positioingMode);
PositioningMode PositioningModeGet();
