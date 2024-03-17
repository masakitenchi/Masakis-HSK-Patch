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
7z a -thash Test.zip.sha256 Test.zip
7z u dist.zip Test.zip.sha256 Test.zip