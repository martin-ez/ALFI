import random, os
import numpy as np
from PIL import Image

def sample(correct, subjects, paths):
    if random.random() < 0.5:
        return sample_dataset(correct, paths[0], subjects)
    else:
        return sample_dataset(correct, paths[1], 30)

def sample_dataset(positive, folder, subjects):
    sbj1 = random.randint(0, subjects)
    sbj2 = random.randint(0, subjects)
    while sbj1 == sbj2:
        sbj2 = random.randint(0, subjects)
    no1 = random.randint(0, subjects)
    no2 = random.randint(0, subjects)
    while no1 == no2:
        no2 = random.randint(0, subjects)

    if positive:
        sbj2 = sbj1

    sample1 = getRGBD(sbj1, no1, folder)
    sample2 = getRGBD(sbj2, no2, folder)

    if sample1 is None or sample2 is None:
        return sample_dataset(positive, folder, subjects)

    return np.array([sample1, sample2])

def getRGBD(sbjNo, imgNo, folder):
    color = get_color(sbjNo, imgNo, folder)
    if color is None:
        return None
    depth = get_channel(sbjNo, imgNo, folder)

    rgbd = np.zeros((100,100,4))
    rgbd[:,:,:3] = color[:,:,:3]
    rgbd[:,:,3] = depth

    return rgbd

def getGDII(sbjNo, imgNo, folder):
    color = get_channel(sbjNo, imgNo, folder, channel='color')
    if color is None:
        return None
    depth = get_channel(sbjNo, imgNo, folder, channel='depth')
    infrared = get_channel(sbjNo, imgNo, folder, channel='infrared')
    index = get_channel(sbjNo, imgNo, folder, channel='index')

    gdii = np.zeros((100,100,4))
    gdii[:,:,0] = color
    gdii[:,:,1] = depth
    gdii[:,:,2] = infrared
    gdii[:,:,3] = index

    return gdii

def get_color(sbjNo, imgNo, folder):
    file = os.path.join(folder, 'sbj-'+str(sbjNo), 'cpt_'+str(imgNo)+'_color.png')
    try:
        color = Image.open(file)
        return np.asarray(color)
    except:
        return None

def get_channel(sbjNo, imgNo, folder, channel='depth'):
    file = os.path.join(folder, 'sbj-'+str(sbjNo), 'cpt_'+str(imgNo)+'_'+channel+'.png')
    try:
        img = Image.open(file)
        if (channel == 'color'):
            img = img.convert('L')
        else:
            img, _, _ = img.split()
        return np.asarray(img)
    except:
        return None
