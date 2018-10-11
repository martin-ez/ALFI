import os, json
from PIL import Image

no_images = 13
infrared_out_size = 75
color_out_size = 200

if __name__ == "__main__":
    for sbj in range(no_images):
        root = os.path.join('Raw_Data', 'sbj-'+str(sbj))
        root_out = os.path.join('TrainingData', 'sbj-'+str(sbj))
        if not os.path.exists(root_out):
            os.makedirs(root_out)
        data = None
        for i in range(3):
            color_image = None
            depth_image = None
            index_image = None
            infrared_image = None
            try:
                with open(os.path.join(root, 'cpt_'+str(i)+'_FaceData.json')) as f:
                    data = json.load(f)
            except FileNotFoundError:
                print("Missing info "+str(i)+" of subject "+str(sbj)+".")
            except IOError:
                print("Missing image "+str(i)+" of subject "+str(sbj)+".")
            if data:
                color_image = Image.open(os.path.join(root, 'cpt_'+str(i)+'_color.png'))
                depth_image = Image.open(os.path.join(root, 'cpt_'+str(i)+'_depth.png'))
                index_image = Image.open(os.path.join(root, 'cpt_'+str(i)+'_index.png'))
                infrared_image = Image.open(os.path.join(root, 'cpt_'+str(i)+'_infrared.png'))

                bb_color = data["BoundingBoxColorSpace"]
                rect_color = [bb_color["Left"], bb_color["Top"], bb_color["Right"], bb_color["Bottom"]]
                rect_color_adj = [0, 0, 0, 0]
                rect_color_adj[0] = int(rect_color[0] + ((rect_color[2]-rect_color[0])/2)-(color_out_size/2))
                rect_color_adj[1] = int(rect_color[1] + ((rect_color[3]-rect_color[1])/2)-(color_out_size/2))
                rect_color_adj[2] = int(rect_color[0] + ((rect_color[2]-rect_color[0])/2)+(color_out_size/2))
                rect_color_adj[3] = int(rect_color[1] + ((rect_color[3]-rect_color[1])/2)+(color_out_size/2))

                bb_infrared = data["BoundingBoxInfraredSpace"]
                rect_infrared = [bb_infrared["Left"], bb_infrared["Top"], bb_infrared["Right"], bb_infrared["Bottom"]]
                rect_infrared_adj = [0, 0, 0, 0]
                rect_infrared_adj[0] = int(rect_infrared[0] + ((rect_infrared[2]-rect_infrared[0])/2)-(infrared_out_size/2))
                rect_infrared_adj[1] = int(rect_infrared[1] + ((rect_infrared[3]-rect_infrared[1])/2)-(infrared_out_size/2))
                rect_infrared_adj[2] = int(rect_infrared[0] + ((rect_infrared[2]-rect_infrared[0])/2)+(infrared_out_size/2))
                rect_infrared_adj[3] = int(rect_infrared[1] + ((rect_infrared[3]-rect_infrared[1])/2)+(infrared_out_size/2))

                cropped_color = color_image.crop(box=rect_color_adj)
                cropped_color.save(os.path.join(root_out, 'cpt_'+str(i)+'_color.png'))
                cropped_depth = depth_image.crop(box=rect_infrared_adj)
                cropped_depth = cropped_depth.resize((color_out_size, color_out_size))
                cropped_depth.save(os.path.join(root_out, 'cpt_'+str(i)+'_depth.png'))
                cropped_index = index_image.crop(box=rect_infrared_adj)
                cropped_index = cropped_index.resize((color_out_size, color_out_size))
                cropped_index.save(os.path.join(root_out, 'cpt_'+str(i)+'_index.png'))
                cropped_infrared = infrared_image.crop(box=rect_infrared_adj)
                cropped_infrared = cropped_infrared.resize((color_out_size, color_out_size))
                cropped_infrared.save(os.path.join(root_out, 'cpt_'+str(i)+'_infrared.png'))
