import numpy as np
import cv2
import time
import os
from KinectController import Kinect

class DataRecollection(object):

    def __init__(self):
        self.kinect = Kinect();
        self.face_cascade = cv2.CascadeClassifier('cascades\data\haarcascade_frontalface_alt2.xml')
        self.out_img_size = 280
        self.out_img_index = 0
        self.next_image_save = 0.0
        self.img_save_cooldown = 1.0

        self.subject_name = 'Sebastian'

    def frame_proccessing(self, frame, fps):
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

        detected_faces = []
        frontal_faces = self.face_cascade.detectMultiScale(gray, scaleFactor=1.5, minNeighbors=5)
        for (x, y, w, h) in frontal_faces:
            new_face = [int(x+(w/2)), int(y+(h/2))]
            detected_faces.append(new_face)

        if (len(detected_faces) > 0):

            xStart = max(0, int(detected_faces[0][0] - (self.out_img_size/2)))
            xEnd = xStart + self.out_img_size;
            yStart = max(0, int(detected_faces[0][1] - (self.out_img_size/2)))
            yEnd = yStart + self.out_img_size;

            color = (200, 0, 255)
            stroke = 2
            cv2.rectangle(frame, (xStart, yStart), (xEnd, yEnd), color, stroke)

            now = time.time()
            if (now > self.next_image_save):
                img_index = ''
                if self.out_img_index < 10:
                    img_index += '00'
                elif self.out_img_index < 100:
                    img_index += '0'
                img_index += str(self.out_img_index)
                path = os.path.join(self.directory, '_'+img_index+'.png')
                subimage = gray[yStart:yEnd, xStart:xEnd]
                cv2.imwrite(path, subimage)
                self.out_img_index = self.out_img_index + 1
                self.next_image_save = now + self.img_save_cooldown

        cv2.imshow('Capture', frame)

    def start_capture(self):
        self.directory = os.path.join('data', self.subject_name)
        if not os.path.exists(self.directory):
            os.makedirs(self.directory)
        self.kinect.capture(self.frame_proccessing)
        cv2.destroyAllWindows()

__main__ = "Kinect Image Recollection"

data = DataRecollection()
data.start_capture()
