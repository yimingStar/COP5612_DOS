#!bash
for var in $(seq 5 $END)
do
   (cd ~/Documents/UFL/2021_Fall/COP5615-DistOperSysPrinc/COP5612_DOS/projects/Project4/; osascript clientSimulator.sh $var)
done
