import random, os
import numpy as np
from PIL import Image

def getRGBD(sbjNo, imgNo, folder):
    color = get_color(sbjNo, imgNo, folder)
    if color is None:
        return None
    depth = get_channel(sbjNo, imgNo, 'depth', folder)

    rgbd = np.zeros((100,100,4))
    rgbd[:,:,:3] = color[:,:,:3]
    rgbd[:,:,3] = depth

    return rgbd

def getGDII(sbjNo, imgNo, folder):
    color = get_channel(sbjNo, imgNo, 'color', folder)
    if color is None:
        return None
    depth = get_channel(sbjNo, imgNo, 'depth', folder)
    infrared = get_channel(sbjNo, imgNo, 'infrared', folder)
    index = get_channel(sbjNo, imgNo, 'index', folder)

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

def get_channel(sbjNo, imgNo, channel, folder):
    file = os.path.join(folder,'sbj-'+str(sbjNo), 'cpt_'+str(imgNo)+'_'+channel+'.png')

    try:
        img = Image.open(file)
        if (channel == 'color'):
            img = img.convert('L')
        else:
            img, _, _ = img.split()
        img = np.asarray(img)
        return img
    except:
        return None
