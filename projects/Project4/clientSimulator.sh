# Mac Script
# on run argv
#     tell application "Terminal"
#         do script "cd ~/Documents/UFL/2021_Fall/COP5615-DistOperSysPrinc/COP5612_DOS/projects/Project4/twitter-client\n" & "dotnet run"
#         delay 1
#         do script "\nCONNECT test_" & (item 1 of argv)
#         delay 1 
#         do script "\nREGISTER test_" & (item 1 of argv)
#     end tell
# end run

on run argv
    tell application "Terminal"
        activate
        do script "cd ~/Documents/UFL/2021_Fall/COP5615-DistOperSysPrinc/COP5612_DOS/projects/Project4/twitter-client\n" & "dotnet run\n" & "CONNECT test_" & (item 1 of argv) & "\nREGISTER test_" & (item 1 of argv) & "\n"
    end tell
end run