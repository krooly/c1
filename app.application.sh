#!/bin/bash

echo "Hello World"

mkdir ~/tmpC1/
cd ~/tmpC1/

wget https://raw.githubusercontent.com/krooly/c1/master/app.application > /dev/null

#cat app.application

sudo apt-get install xml2

${stringZ:(-4)}
versionApp=`xml2 < app.application | grep assembly/*assemblyIdentity/@version`

num=`expr index $versionApp '='`
versionApp=${versionApp:(-num)}

echo $versionApp

rm -rf ~/tmpC1/
