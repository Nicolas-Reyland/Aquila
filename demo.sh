#!/bin/bash
src_code=$1
echo "Source Code of ${src_code}:"
echo "========================================================"
cat "${src_code}"
echo "========================================================"
echo "press Enter to run"
read
mono program.exe "${src_code}"
