# Python Scripts for rimworld modding

This folder contains several "useful" scripts which could help when modding rimworld.

## Translation Cleaner:
Rimworld provides a powerful API for translation, but when extracting translation file, it forgets to take the feature of different languages into account. Take Chinese as an example:
	
	1. The singular form and plural form of a noun do not differ that far from eath other
	2. The variation for different life stage and sex is also not that diversed (Simply add small, male, female to **Cow**(牛) and you can get cuff, bull, cow, etc.)
	3. Noun can usually be used as an adjective, no prefix, postfix, etc.

This script will remove these tags in a tranlation file:
	
	1. if the tag contains "Plural" (example 1)
	2. if the tag contains "lifeStages" (example 2)
	3. if the tag contains "Mech" as well as "Male" or "Female" (Why on earth should mechanoids have sex?)
	4. if the tag contains "stuffAdjective" (example 3)

There maybe be more feature to come in the future

## Translation Extractor:
I know I know, there is already RimTrans and official tranlation export tool built in-game, but they all have some problems:
	
	1. Rimtrans cannot detect patches that changes the original label/description of a def
	2. Rimtrans, as I've mentioned in Section Translation Cleaner, sometimes exports useless translation
	3. Official export tool only works with vanilla

So this is my solution. This script can detect patches that modify label/description and export them to respect folders. It cannot take XML inheritance into account now, but it will.

## PatchOperationGenerator:
New modders often have a hard time when trying to understand how PatchOperations work. But now you can simply modify the original file (make sure you have a backup ofc), and use this script to diff the original and the modified file. It will output the eqivalent xml file with PatchOperations, so you can cut & paste them into a separate mod, without having to worry about what if the mod gets updated.



# Current TO-DO List:

- [] XML inheritance feature in Translation Extractor
- [] GUI interface
- [] TO-DO