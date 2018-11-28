import random, os, argparse
import numpy as np
from PIL import Image

def process_color(imgNo, dataset, in_path, out_path):
    file = os.path.join(in_path, 'cpt_'+str(imgNo)+'_color_i.png')
    if dataset == 'DS':
        prefix = '00'
        if imgNo > 9:
            prefix = '0'
        file = os.path.join(in_path, prefix+str(imgNo)+'_2_c.bmp')

    try:
        color = Image.open(file)
    except:
        return False

    if dataset == 'DS':
        color = color.resize((640,480), Image.LANCZOS)
        color = color.crop(box=(220, 120, 420, 320))
        color = color.resize((100,100), Image.LANCZOS)
    else:
        color = color.resize((718,404), Image.LANCZOS)
        color = color.crop(box=(309, 102, 409, 202))
    color.save(os.path.join(out_path, 'cpt_'+str(imgNo)+'_color.png'))
    return True

def process_depth(imgNo, dataset, in_path, out_path):
    depth_file = os.path.join(in_path, 'cpt_'+str(imgNo)+'_depth_d.dat')
    mat=np.zeros((424,512), dtype='float32')
    if dataset == 'DS':
        mat=np.zeros((480,640), dtype='float32')
        prefix = '00'
        if imgNo > 9:
            prefix = '0'
        depth_file = os.path.join(in_path, prefix+str(imgNo)+'_2_d.dat')

    i=0
    j=0
    with open(depth_file) as file:
        for line in file:
            vals = line.split('\t')
            for val in vals:
                if val == "\n": continue
                val = int(val)
                if val < 400: val = 400
                if val > 3000: val = 3000
                mat[i][j]= lerp(val)
                j+=1
                if dataset == 'DS':
                    j=j%640
                else:
                    j=j%512

            i+=1
    image = np.zeros((mat.shape[0], mat.shape[1], 3))
    image[:,:,0] = mat
    image[:,:,1] = mat
    image[:,:,2] = mat

    pil_image = Image.fromarray(np.uint8(image*255))

    if dataset == 'DS':
        pil_image = pil_image.crop(box=(220, 120, 420, 320))
        pil_image = pil_image.resize((100,100), Image.LANCZOS)
    else:
        pil_image = pil_image.crop(box=(190, 102, 290, 202))

    pil_image.save(os.path.join(out_path, 'cpt_'+str(imgNo)+'_depth.png'))

def lerp(val):
    return float((val-400)/(3000-400))

def process_infrared(imgNo, in_path, out_path):
    file = os.path.join(in_path, 'cpt_'+str(imgNo)+'_infrared_i.png')

    infrared = Image.open(file)
    infrared = infrared.crop(box=(190, 102, 290, 202))

    infrared.save(os.path.join(out_path, 'cpt_'+str(imgNo)+'_infrared.png'))

def process_index(imgNo, in_path, out_path):
    file = os.path.join(in_path, 'cpt_'+str(imgNo)+'_index_i.png')

    index = Image.open(file)
    index = index.crop(box=(190, 102, 290, 202))

    index.save(os.path.join(out_path, 'cpt_'+str(imgNo)+'_index.png'))

def preprocess(in_path, out_path, dataset, imgNo):
    process_color(imgNo, dataset, in_path, out_path)
    process_depth(imgNo, dataset, in_path, out_path)
    if dataset == 'DC':
        process_infrared(imgNo, in_path, out_path)
        process_index(imgNo, in_path, out_path)

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--data', help="Dataset to preprocess")
    args = vars(parser.parse_args())
    dataset = args['data']

    raw_path = os.path.join(os.path.dirname(__file__), 'data', 'Raw_'+dataset)
    process_path = os.path.join(os.path.dirname(__file__), 'data', dataset)

    sbj = 0
    while os.path.exists(os.path.join(raw_path, 'sbj-'+str(sbj))):
        print(' * Starting subject '+str(sbj))
        in_path = os.path.join(raw_path, 'sbj-'+str(sbj))
        out_path = os.path.join(process_path, 'sbj-'+str(sbj))
        if not os.path.exists(out_path):
            os.makedirs(out_path)
        sbjRange = range(0, 9)
        if dataset == 'DS':
            sbjRange = range(1, 18)
        for i in sbjRange:
            if not process_color(i, dataset, in_path, out_path):
                print(' -- Missing capture '+str(i)+' of subject '+str(sbj))
                continue
            process_depth(i, dataset, in_path, out_path)
            if dataset == 'DC':
                process_infrared(i, in_path, out_path)
                process_index(i, in_path, out_path)
        print(' ++ completed')
        sbj = sbj + 1
