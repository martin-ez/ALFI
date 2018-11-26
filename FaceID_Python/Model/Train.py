import os
from FaceID import FaceID
from SampleGenerator import sample, sample_dr

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--save', help="Save folder")

    save_name = args['save']

    if not save_name:
        save_name = 'model_weigths'

    faceID = FaceID()
    faceID.model.summary()
    faceID.train(40, save_name, verbose=False)
