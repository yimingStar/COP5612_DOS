#!/bin/bash 

osascript -e 'tell app "Terminal"
    do script "
cd -- ~/Documents/UFL/2021_Fall/COP5615-DistOperSysPrinc/COP5612_DOS/projects/Project4/twitter-client\n
dotnet run\n
CONNECT test\n"
end tell'


    