#!/bin/bash
PATH=/bin
if [ $# != 1 ]; then
  /bin/echo "You have to pass the path to the script you want to demo as the only argument"
  exit
fi
src_code=$1
/bin/echo "The compiled interpreter should be in the current directory. mono shoud be installed too."
/bin/echo "Source Code of ${src_code}:"
/bin/echo "========================================================"
/bin/cat "${src_code}"
/bin/echo "========================================================"
/bin/echo "press Enter to run"
read
/bin/mono program.exe "${src_code}"
