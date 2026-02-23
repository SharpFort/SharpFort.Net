#!/bin/bash
./end.sh
nohup dotnet SharpFort.Web.dll > /dev/null 2>&1 &
echo "Sf-启动成功!"
