# BLE anchor firmware

Here you can see the projects for Keil MDK-ARM uVision and STM32CubeMX. 

The MCU runs three cooperative tasks (nonos code):

- BleTask
- ZigBeeTask
- ServiceUartIFTask

## Ble task

Ble task controls four BL652 modules. It starts transmitting/receiving mode deepening on positioning mode (Remote and Self-positioning).

During remote positioning operation it awaits for reports send from BL652 module via uart. Because each advertisementshould be received by all modules we should get four uart messages, each with RSS reading per modules. Due to processing times these messages are not in sync. Therefore the Ble task implements queue. The key for items in queue is address or id of BLE device. On Ble uart report received, the queue is searched and the relevant RSS field in filled. In the device address in not found a new item is created. If all RSS fields are not filled in 4 milliseconds, the report is send regardless of missing readings as they are considered to be not received by the BL652.

## ZigBeeTask

Initializes the communication with ZigBee module ETRX357 and provides interface for control of Anchor.

## ServiceUartIFTask

This task is used as debug service, it saves the messages to buffer and than outputs to uart. It works similarly like prints to standard console output.

