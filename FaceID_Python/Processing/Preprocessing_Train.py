import os
from PIL import Image
import numpy as np

no_images = 31
infrared_out_size = 75
color_out_size = 200

def process(root, root_out, imgNo):
    prefix = '00'
    if imgNo >= 9:
        prefix = '0'
    color_file = os.path.join(root, prefix+str(imgNo+1)+'_2_c.bmp')
    depth_file = os.path.join(root, prefix+str(imgNo+1)+'_2_d.dat')
    color_image = get_color(color_file)
    color_image.save(os.path.join(root_out, 'cpt_'+str(imgNo)+'_color.png'))
    depth_image = get_depth(depth_file)
    depth_image.save(os.path.join(root_out, 'cpt_'+str(imgNo)+'_depth.png'))

def get_color(color_file):
    img = Image.open(color_file)
    img.thumbnail((640,480))
    img = np.asarray(img)
    img = img[140:340,220:420]
    return Image.fromarray(img)

def get_depth(depth_file):
    mat=np.zeros((480,640), dtype='float32')
    i=0
    j=0
    with open(depth_file) as file:
        for line in file:
            vals = line.split('\t')
            for val in vals:
                if val == "\n": continue
                val = int(val)
                if val < 400: val = 400
                if val > 3000: val = 1200
                mat[i][j]= lerp(val)
                j+=1
                j=j%640

            i+=1
    mat=mat[140:340,220:420]
    image = np.zeros((200, 200, 3))
    image[:,:,0] = mat
    image[:,:,2] = mat
    image[:,:,1] = mat
    return Image.fromarray(np.uint8(image*255))

def lerp(val):
    return float((val-400)/(3000-400))

if __name__ == "__main__":
    
    for sbj in range(no_images):
        root = os.path.join('data', 'Raw_Train', 'sbj-'+str(sbj))
        root_out = os.path.join('data', 'training', 'sbj-'+str(sbj))
        if not os.path.exists(root_out):
            os.makedirs(root_out)
        for i in range(17):
            print('sbj: '+str(sbj)+' img: '+str(i))
            process(root, root_out, i)
