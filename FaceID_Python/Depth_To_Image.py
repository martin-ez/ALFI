import os, argparse
from PIL import Image
import numpy as np

def process(depth_file, img_out):
    depth_image = get_depth(depth_file)
    depth_image.save(img_out)

def get_depth(depth_file):
    mat=np.zeros((424,512), dtype='float32')
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
                j=j%512

            i+=1
    image = np.zeros((424, 512, 3))
    image[:,:,0] = mat
    image[:,:,1] = mat
    image[:,:,2] = mat
    return Image.fromarray(np.uint8(image*255))

def lerp(val):
    return float((val-400)/(3000-400))

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--in', help="Path to the input data file")
    parser.add_argument('--out', help="Path to the output image")
    args = vars(parser.parse_args())

    input = args['in']
    output = args['out']

    if input and output:
        process(input, output)
