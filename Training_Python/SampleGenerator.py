import random, os
import numpy as np
from PIL import Image

def sample(correct, validation=False):
    data_folder = os.path.join(os.path.dirname(__file__), 'data', 'training')
    sbj1 = random.randint(0,30)
    sbj2 = random.randint(0,30)
    while sbj1 == sbj2:
        sbj2 = random.randint(0,30)
    no1 = random.randint(0, 10)
    no2 = random.randint(0, 10)
    while no1 == no2:
        no2 = random.randint(0,10)

    if validation:
        #data_folder = os.path.join(os.path.dirname(__file__), 'data', 'validation')
        #sbj1 = random.randint(0,10)
        #sbj2 = random.randint(0,10)
        #while sbj1 == sbj2:
            #sbj2 = random.randint(0,10)
        no1 = random.randint(11, 16)
        no2 = random.randint(11, 16)
        while no1 == no2:
            no2 = random.randint(11,16)

    folder1 = os.path.join(data_folder, 'sbj-'+str(sbj1))
    folder2 = os.path.join(data_folder, 'sbj-'+str(sbj2))
    if correct:
        folder2 = folder1

    sample1 = getRGBD(folder1, no1)
    sample2 = getRGBD(folder2, no2)

    return np.array([sample1, sample2])

def getRGBD(folder, imgNo):
    color = Image.open(os.path.join(folder, 'cpt_'+str(imgNo)+'_color.png'))
    depth = Image.open(os.path.join(folder, 'cpt_'+str(imgNo)+'_depth.png'))
    depth, _, _ = depth.split()
    color = np.array(color)
    depth = np.array(depth)

    rgbd = np.zeros((200,200,4))
    rgbd[:,:,:3] = color[:,:,:3]
    rgbd[:,:,3] = depth

    return rgbd
