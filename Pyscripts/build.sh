#!/bin/bash
pip install pyinstaller
pyinstaller __main__.py --windowed -n Pyscripts -y

echo "zip into a zip file(y/N)?"
read ans
if [ "$ans" != "y" ]; then
	echo "Aborted"
	exit 1
fi
cd dist
7z u Test.zip Pyscripts