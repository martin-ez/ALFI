import random, os
import numpy as np
from PIL import Image

def sample_both(correct, validation=False):
    if random.random() < 0.25:
        return sample_dr(correct, validation=validation)
    else:
        return sample(correct, validation=validation)

def sample_dr(correct, validation=False):
    data_folder = os.path.join(os.path.dirname(__file__), 'data', 'validation')
    sbj1 = random.randint(0, 7)
    sbj2 = random.randint(0, 7)
    while sbj1 == sbj2:
        sbj2 = random.randint(0, 7)
    no1 = random.randint(0, 2)
    no2 = random.randint(0, 2)
    while no1 == no2:
        no2 = random.randint(0, 2)

    if validation:
        sbj1 = random.randint(8, 10)
        sbj2 = random.randint(8, 10)
        while sbj1 == sbj2:
            sbj2 = random.randint(8, 10)

    folder1 = os.path.join(data_folder, 'sbj-'+str(sbj1))
    folder2 = os.path.join(data_folder, 'sbj-'+str(sbj2))
    if correct:
        folder2 = folder1

    sample1 = getRGBD(folder1, no1)
    sample2 = getRGBD(folder2, no2)

    return np.array([sample1, sample2])

def sample(correct, validation=False):
    data_folder = os.path.join(os.path.dirname(__file__), 'data', 'training')
    sbj1 = random.randint(0, 25)
    sbj2 = random.randint(0, 25)
    while sbj1 == sbj2:
        sbj2 = random.randint(0, 25)
    no1 = random.randint(0, 16)
    no2 = random.randint(0, 16)
    while no1 == no2:
        no2 = random.randint(0,16)

    if validation:
        #data_folder = os.path.join(os.path.dirname(__file__), 'data', 'validation')
        sbj1 = random.randint(26, 30)
        sbj2 = random.randint(26, 30)
        while sbj1 == sbj2:
            sbj2 = random.randint(26, 30)
        #no1 = random.randint(0, 2)
        #no2 = random.randint(0, 2)
        #while no1 == no2:
            #no2 = random.randint(0, 2)

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
