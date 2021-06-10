# Smart Basic Scripts for BL652

Here we present smart basic scripts which describe the operation of BL652 module. There are two scripts. Depending on positioning mode the relevant script is launhed.

## adscan.advert.display

This script runs in Remote positioning mode. it outputs advertisement reports to uart (including RSS reading).

## sAnchor

sAnchor runs in Self-positioning mode. It periodically sends advertisements which include Anchor identification.

## $autorun$

Runs in Tag0 type. It only transmits advertisement reports every 100 ms.