import random, os
import numpy as np
from PIL import Image

img_repo_path = os.path.join(os.path.dirname(__file__), 'data', 'repo')

def sample_dc(positive, validation=False):
    sbj1 = random.randint(0, 10)
    sbj2 = random.randint(0, 10)
    while sbj1 == sbj2:
        sbj2 = random.randint(0, 10)
    no1 = random.randint(0, 8)
    no2 = random.randint(0, 8)
    while no1 == no2:
        no2 = random.randint(0, 8)

    if validation:
        sbj1 = random.randint(11, 14)
        sbj2 = random.randint(11, 14)
        while sbj1 == sbj2:
            sbj2 = random.randint(1, 14)

    if positive:
        sbj2 = sbj1

    sample1 = getRGBD(sbj1, no1, 'DC')
    sample2 = getRGBD(sbj2, no2, 'DC')

    if sample1 is None or sample2 is None:
        return sample_dc(positive, validation=validation)

    return np.array([sample1, sample2])

def sample_ds(positive, validation=False):
    sbj1 = random.randint(0, 25)
    sbj2 = random.randint(0, 25)
    while sbj1 == sbj2:
        sbj2 = random.randint(0, 25)
    no1 = random.randint(0, 16)
    no2 = random.randint(0, 16)
    while no1 == no2:
        no2 = random.randint(0,16)

    if validation:
        sbj1 = random.randint(26, 30)
        sbj2 = random.randint(26, 30)
        while sbj1 == sbj2:
            sbj2 = random.randint(26, 30)

    if positive:
        sbj2 = sbj1

    sample1 = getRGBD(sbj1, no1, 'DS')
    sample2 = getRGBD(sbj2, no2, 'DS')

    return np.array([sample1, sample2])

def sample_ir(sbj):
    return getRGBD(sbj, 0, 'IR')

def getRGBD(sbjNo, imgNo, dataset='DC'):
    color = getProcessColor(sbjNo, imgNo, dataset)
    if color is None:
        return None
    depth = getProcessDepth(sbjNo, imgNo, dataset)

    rgbd = np.zeros((100,100,4))
    if dataset == 'DS':
        rgbd = np.zeros((200,200,4))

    rgbd[:,:,:3] = color[:,:,:3]
    rgbd[:,:,3] = depth

    return rgbd

def getProcessColor(sbjNo, imgNo, dataset):
    folder = os.path.join(os.path.dirname(__file__), 'data', dataset, 'sbj-'+str(sbjNo))
    file = os.path.join(folder, 'cpt_'+str(imgNo)+'_color_i.png')
    if dataset == 'DS':
        prefix = '00'
        if imgNo >= 9:
            prefix = '0'
        file = os.path.join(folder, prefix+str(imgNo+1)+'_2_c.bmp')
    elif dataset == 'IR':
        file = os.path.join(img_repo_path, 'sbj_'+str(sbjNo)+'_color.png')

    try:
        color = Image.open(file)
    except:
        return None

    if dataset == 'DS':
        color = color.resize((640,480), Image.LANCZOS)
        color = color.crop(box=(140, 220, 340, 420))
        color = color.resize((100,100), Image.LANCZOS)
        return np.asarray(color)
    else:
        color = color.resize((718,404), Image.LANCZOS)
        color = color.crop(box=(309, 102, 409, 202))
        return np.asarray(color)

def getProcessDepth(sbjNo, imgNo, dataset):
    folder = os.path.join(os.path.dirname(__file__), 'data', dataset, 'sbj-'+str(sbjNo))
    depth_file = os.path.join(folder, 'cpt_'+str(imgNo)+'_depth_d.dat')
    mat=np.zeros((424,512), dtype='float32')
    if dataset == 'DS':
        mat=np.zeros((480,640), dtype='float32')
        prefix = '00'
        if imgNo >= 9:
            prefix = '0'
        depth_file = os.path.join(folder, prefix+str(imgNo+1)+'_2_d.dat')
    elif dataset == 'IR':
        file = os.path.join(img_repo_path, 'sbj_'+str(sbjNo)+'_depth.dat')

    i=0
    j=0
    with open(depth_file) as file:
        for line in file:
            vals = line.split('\t')
            for val in vals:
                if val == "\n": continue
                mat[i][j]= int(val)
                j+=1
                if dataset == 'DS':
                    j=j%640
                else:
                    j=j%512

            i+=1

    if dataset == 'DS':
        return mat[140:340,220:420]
    else:
        return mat[190:290,102:202]
