#!/bin/bash

# add execute permission and run the app as regular user but with sudo permissions on the current shell
# DO NOT execute with `sudo ./DeadlockServerPicker` since it prevents `xdg-open` from accessing user environment variables

sudo chmod +x DeadlockServerPicker

./DeadlockServerPicker
