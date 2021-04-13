import time
from easygopigo3 import EasyGoPiGo3

gpg = EasyGoPiGo3()

gpg.reset_all()
time.sleep(1)  # let's give the reset_all() some time to finish
radius = 30
gpg.set_speed(300)
print("Warning: The robot is about to move left. ")
gpg.orbit(-270, radius) # to rotate to the left
print("Warning: The robot is about to move forward. ")
gpg.drive_cm(radius * 2) # move forward
print("Warning: The robot is about to move right. ")
gpg.orbit(270, radius) # to rotate to the right
print("Warning: The robot is about to move forward. ")
gpg.drive_cm(radius * 2) # move forward