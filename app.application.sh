#!/bin/bash

echo "Hello World"

mkdir ~/tmpC1/
cd ~/tmpC1/

wget https://raw.githubusercontent.com/krooly/c1/master/app.application

cat app.application

cd ..
rm -rf ~/tmpC1/
