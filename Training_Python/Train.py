from FaceID import FaceID

if __name__ == "__main__":
    faceID = FaceID()
    faceID.model.summary()
    faceID.train(30, 1)
