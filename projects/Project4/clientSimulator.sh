# Mac Script
on run argv
    tell application "Terminal"
        reopen
        activate
        do script "cd ~/Documents/UFL/2021_Fall/COP5615-DistOperSysPrinc/COP5612_DOS/projects/Project4/twitter-client\n" & "dotnet run\n" in window 1
        do script "CONNECT test_" & (item 1 of argv) in window 1
        delay 3 
        do script "REGISTER test_" & (item 1 of argv) in window 1
        delay 3 
        do script "SUBSCRIBE test" in window 1
    end tell
end run