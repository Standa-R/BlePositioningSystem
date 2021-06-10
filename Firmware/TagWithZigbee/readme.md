# BLE tag1 firmware (with Zigbee)

Here you can see the projects for Keil MDK-ARM uVision and STM32CubeMX. 

Tag1 uses evaluation board NUCLEO-F429ZI.

The MCU runs three cooperative tasks (nonos code):

- BleTask
- ZigBeeTask
- ServiceUartIFTask

## Ble task

Ble task controls a BL652 modules. It starts transmitting/receiving mode deepening on positioning mode (Remote and Self-positioning).

## ZigBeeTask

Initializes the communication with ZigBee module ETRX357 and provides interface for control of the Tag.

## ServiceUartIFTask

This task is used as debug service, it saves the messages to buffer and than outputs to uart. It works similarly like prints to standard console output.

