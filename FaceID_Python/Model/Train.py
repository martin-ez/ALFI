import os, argparse
from FaceID import FaceID

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--save', help="Save folder")
    args = vars(parser.parse_args())

    save_name = args['save']

    if not save_name:
        save_name = 'training'

    faceID = FaceID()
    faceID.model.summary()
    faceID.train(100, save_name, load=True)
