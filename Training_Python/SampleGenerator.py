import random, os
import numpy as np
from PIL import Image

data_folder = os.path.join(os.path.dirname(__file__), 'TrainingData')

def sample(correct=True, val=False, d_channel='depth'):
    sample1 = None
    sample2 = None
    sbj = random.randint(0,11)
    sbj2 = random.randint(0,11)
    while sbj == sbj2:
        sbj2 = random.randint(0,11)
    if sbj == 3 or sbj2 == 3:
        val = False
    if correct and not val:
        folder = os.path.join(data_folder, 'sbj-'+str(sbj))

        sample1 = getRGBD(folder, 0, d_channel)
        sample2 = getRGBD(folder, 1, d_channel)
        return np.array([sample1, sample2])

    if val and not correct:
        folder = os.path.join(data_folder, 'sbj-'+str(sbj))
        folder2 = os.path.join(data_folder, 'sbj-'+str(sbj2))

        sample1 = getRGBD(folder, 2, d_channel)
        sample2 = getRGBD(folder2, 2, d_channel)
        return np.array([sample1, sample2])

    if val and correct:
        folder = os.path.join(data_folder, 'sbj-'+str(sbj))
        imgNo = random.randint(0, 1)

        sample1 = getRGBD(folder, imgNo, d_channel)
        sample2 = getRGBD(folder, 2, d_channel)
        return np.array([sample1, sample2])

    if not val and not correct:
        folder = os.path.join(data_folder, 'sbj-'+str(sbj))
        folder2 = os.path.join(data_folder, 'sbj-'+str(sbj2))
        imgNo = random.randint(0, 1)
        imgNo2 = random.randint(0, 1)

        sample1 = getRGBD(folder, imgNo, d_channel)
        sample2 = getRGBD(folder2, imgNo2, d_channel)
        return np.array([sample1, sample2])

def getRGBD(folder, imgNo, d_channel):
    color = Image.open(os.path.join(folder, 'cpt_'+str(imgNo)+'_color.png'))
    depth = Image.open(os.path.join(folder, 'cpt_'+str(imgNo)+'_'+d_channel+'.png'))
    depth, _, _ = depth.split()
    color = np.array(color)
    depth = np.array(depth)

    rgbd = np.zeros((200,200,4))
    rgbd[:,:,:3] = color[:,:,:3]
    rgbd[:,:,3] = depth

    return rgbd
