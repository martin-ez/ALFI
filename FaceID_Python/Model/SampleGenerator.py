import random, os
import numpy as np
from PIL import Image

def sample_both(correct, validation=False):
    if random.random() < 0.5:
        return sample_dc(correct, validation=validation)
    else:
        return sample_ds(correct, validation=validation)

def sample_dc(positive, validation=False):
    sbj1 = random.randint(0, 14)
    sbj2 = random.randint(0, 14)
    while sbj1 == sbj2:
        sbj2 = random.randint(0, 14)
    no1 = random.randint(1, 8)
    no2 = random.randint(1, 8)
    while no1 == no2:
        no2 = random.randint(1, 8)

    if validation:
        no1 = 0
        no2 = random.randint(1, 8)

    if positive:
        sbj2 = sbj1

    sample1 = getGDII(sbj1, no1)
    sample2 = getGDII(sbj2, no2)

    if sample1 is None or sample2 is None:
        return sample_dc(positive, validation=validation)

    return np.array([sample1, sample2])

def sample_ds(positive, validation=False):
    sbj1 = random.randint(0, 30)
    sbj2 = random.randint(0, 30)
    while sbj1 == sbj2:
        sbj2 = random.randint(0, 30)
    no1 = random.randint(1, 17)
    no2 = random.randint(1, 17)
    while no1 == no2:
        no2 = random.randint(1, 17)

    if validation:
        sbj1 = random.randint(26, 30)
        sbj2 = random.randint(26, 30)
        while sbj1 == sbj2:
            sbj2 = random.randint(26, 30)

    if positive:
        sbj2 = sbj1

    sample1 = getRGBD(sbj1, no1, dataset='DS')
    sample2 = getRGBD(sbj2, no2, dataset='DS')

    if sample1 is None or sample2 is None:
        return sample_ds(positive, validation=validation)

    return np.array([sample1, sample2])

def getRGBD(sbjNo, imgNo, dataset='DC', channel='depth'):
    color = get_color(sbjNo, imgNo, dataset)
    if color is None:
        return None
    depth = get_channel(sbjNo, imgNo, dataset, channel=channel)

    rgbd = np.zeros((100,100,4))
    rgbd[:,:,:3] = color[:,:,:3]
    rgbd[:,:,3] = depth

    return rgbd

def getGDII(sbjNo, imgNo):
    color = get_channel(sbjNo, imgNo, 'DC', channel='color')
    if color is None:
        return None
    depth = get_channel(sbjNo, imgNo, 'DC', channel='depth')
    infrared = get_channel(sbjNo, imgNo, 'DC', channel='infrared')
    index = get_channel(sbjNo, imgNo, 'DC', channel='index')

    gdii = np.zeros((100,100,4))
    gdii[:,:,0] = color
    gdii[:,:,1] = depth
    gdii[:,:,2] = infrared
    gdii[:,:,3] = index

    return gdii

def get_color(sbjNo, imgNo, dataset):
    file = os.path.join(os.path.dirname(__file__), 'data', dataset, 'sbj-'+str(sbjNo), 'cpt_'+str(imgNo)+'_color.png')
    if dataset == 'IR':
        file = os.path.join(img_repo_path, 'sbj-'+str(sbjNo), 'cpt_'+str(imgNo)+'_color.png')

    try:
        color = Image.open(file)
        return np.asarray(color)
    except:
        return None

def get_channel(sbjNo, imgNo, dataset, channel='depth', normalize=False):
    file = os.path.join(os.path.dirname(__file__), 'data', dataset, 'sbj-'+str(sbjNo), 'cpt_'+str(imgNo)+'_'+channel+'.png')
    if dataset == 'IR':
        file = os.path.join(img_repo_path, 'sbj-'+str(sbjNo), 'cpt_'+str(imgNo)+'_'+channel+'.png')

    try:
        img = Image.open(file)
        if (channel == 'color'):
            img = img.convert('L')
        else:
            img, _, _ = img.split()
        img = np.asarray(img)
        if normalize:
            return normalize(img)
        else:
            return img
    except:
        return None

def normalize(mat):
    center = mat[50, 50]
    for i in range(0,100):
        for j in range(0,100):
            mat[i, j] = mat[i, j] - center
    return mat
