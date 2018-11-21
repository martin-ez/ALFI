import os
from FaceID import FaceID
from SampleGenerator import sample, sample_dr

if __name__ == "__main__":
    faceID = FaceID()
    faceID.model.summary()
    faceID.train(40, verbose=False)
    #faceID.load(os.path.join('saved_models','faceID_weights'))

    correct_v = 0
    correct_f = 0

    for i in range(50):
        s = sample(True, validation=True)
        if faceID.predict(s, threshold=0.35):
            correct_v = correct_v + 1;

    for i in range(50):
        s = sample(False, validation=True)
        if not faceID.predict(s, threshold=0.35):
            correct_f = correct_f + 1;

    print('Correct dataset: '+str(correct_v+correct_f)+'/100')
    print('  - Correct pos: '+str(correct_v)+'/50')
    print('  - Correct neg: '+str(correct_f)+'/50')

    correct_v = 0
    correct_f = 0

    for i in range(50):
        s = sample_dr(True, validation=True)
        if faceID.predict(s, threshold=0.35):
            correct_v = correct_v + 1;

    for i in range(50):
        s = sample_dr(False, validation=True)
        if not faceID.predict(s, threshold=0.35):
            correct_f = correct_f + 1;

    print('Correct DR: '+str(correct_v+correct_f)+'/100')
    print('  - Correct pos: '+str(correct_v)+'/50')
    print('  - Correct neg: '+str(correct_f)+'/50')
