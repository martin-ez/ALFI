from tensorflow.keras.models import Sequential, Model
from tensorflow.keras.layers import Dense, Activation, Flatten, Dropout, Lambda, ELU, concatenate, GlobalAveragePooling2D, Input, BatchNormalization, SeparableConv2D, Subtract, concatenate
from tensorflow.keras.activations import relu, softmax
from tensorflow.keras.layers import Convolution2D, MaxPooling2D, AveragePooling2D
from tensorflow.keras.optimizers import RMSprop, SGD
from tensorflow.train import AdamOptimizer as Adam
from tensorflow.keras.regularizers import l2
from tensorflow.keras.callbacks import ModelCheckpoint
from tensorflow.keras import backend as K
from tensorflow import Graph, Session
import numpy as np
from SampleGenerator import sample
import csv, os

def euclidean_distance(inputs):
    assert len(inputs) == 2, \
        'Euclidean distance needs 2 inputs, %d given' % len(inputs)
    u, v = inputs
    return K.sqrt(K.sum((K.square(u - v)), axis=1, keepdims=True))

def contrastive_loss(y_true,y_pred):
    margin=1.
    return K.mean((1. - y_true) * K.square(y_pred) + y_true * K.square(K.maximum(margin - y_pred, 0.)))

def fire(x, squeeze=16, expand=64):
    x = Convolution2D(squeeze, (1,1), padding='valid')(x)
    x = Activation('relu')(x)

    left = Convolution2D(expand, (1,1), padding='valid')(x)
    left = Activation('relu')(left)

    right = Convolution2D(expand, (3,3), padding='same')(x)
    right = Activation('relu')(right)

    x = concatenate([left, right], axis=3)
    return x

def squeezeNet():
    img_input=Input(shape=(100,100,4))

    x = Convolution2D(64, (5, 5), strides=(2, 2), padding='valid')(img_input)
    x = BatchNormalization()(x)
    x = Activation('relu')(x)
    x = MaxPooling2D(pool_size=(3, 3), strides=(2, 2))(x)
    x = fire(x, squeeze=16, expand=16)
    x = fire(x, squeeze=16, expand=16)
    x = MaxPooling2D(pool_size=(3, 3), strides=(2, 2))(x)
    x = fire(x, squeeze=32, expand=32)
    x = fire(x, squeeze=32, expand=32)
    x = MaxPooling2D(pool_size=(3, 3), strides=(2, 2))(x)
    x = fire(x, squeeze=48, expand=48)
    x = fire(x, squeeze=48, expand=48)
    x = fire(x, squeeze=64, expand=64)
    x = fire(x, squeeze=64, expand=64)
    x = Dropout(0.2)(x)
    x = Convolution2D(512, (1, 1), padding='same')(x)
    out = Activation('relu')(x)

    return Model(img_input, out)

def faceIDNet():
    im_in = Input(shape=(100,100,4))

    modelsqueeze = squeezeNet()
    x1 = modelsqueeze(im_in)
    x1 = Flatten()(x1)
    x1 = Dense(512, activation="relu")(x1)
    x1 = Dropout(0.2)(x1)
    feat_x = Dense(128, activation="linear")(x1)
    feat_x = Lambda(lambda  x: K.l2_normalize(x,axis=1))(feat_x)
    model_top = Model(inputs = [im_in], outputs = feat_x)

    im_in1 = Input(shape=(100,100,4))
    im_in2 = Input(shape=(100,100,4))

    feat_x1 = model_top(im_in1)
    feat_x2 = model_top(im_in2)

    lambda_merge = Lambda(euclidean_distance)([feat_x1, feat_x2])

    model_final = Model(inputs = [im_in1, im_in2], outputs = lambda_merge)

    adam = Adam()

    sgd = SGD(lr=0.001, momentum=0.9)

    model_final.compile(optimizer=adam, loss=contrastive_loss)

    return model_final

def generator(batch_size, path):
    while 1:
        x=[]
        y=[]
        switch=True
        for _ in range(batch_size):
            if switch:
                x.append(sample(True, path))
                y.append(np.array([0.]))
            else:
                x.append(sample(False, path))
                y.append(np.array([1.]))
            switch=not switch
        x = np.asarray(x)
        y = np.asarray(y)
        yield [x[:,0],x[:,1]],y

class FaceID:

    def __init__(self):
        self.to_identify_path = os.path.join(os.path.abspath(os.sep), 'ALFI_Data','To_Identify', 'Raw')
        self.to_identify_processed_path = os.path.join(os.path.abspath(os.sep), 'ALFI_Data','To_Identify', 'Processed')
        self.to_process_path = os.path.join(os.path.abspath(os.sep), 'ALFI_Data','To_Process')
        self.dataset_path = os.path.join(os.path.abspath(os.sep), 'ALFI_Data','Dataset','DC')
        self.dataset_ds_path = os.path.join(os.path.abspath(os.sep), 'ALFI_Data','Dataset','DS')
        self.weigths_path = os.path.join(os.path.abspath(os.sep), 'ALFI_Data','Weigths')

        self.current_sbj = 0
        while(os.path.exists(os.path.join(self.dataset_path,'sbj-'+str(self.current_sbj)))):
            self.current_sbj = self.current_sbj + 1

        self.graph = Graph()
        with self.graph.as_default():
            self.session = Session()
            with self.session.as_default():
                self.model = faceIDNet()

    def train(self, epochs, save_name, load=False):
        if load:
            self.load(save_name)
        K.set_session(self.session)
        with self.graph.as_default():
            gen = generator(24, self.dataset_ds_path)
            save_folder = os.path.join(self.weigths_path, save_name)
            if not os.path.exists(save_folder):
                os.makedirs(save_folder)
            cp_callback = ModelCheckpoint(os.path.join(save_folder, 'faceID_weights'), save_weights_only=True)
            self.model.fit_generator(gen, steps_per_epoch=30, epochs=epochs, validation_steps=20, callbacks=[cp_callback])
            lossTrain = self.model.evaluate_generator(gen, steps=30)
            print('* - Loss: '+str(lossTrain))

    def predict(self, inputs, threshold=0.2):
        K.set_session(self.session)
        with self.graph.as_default():
            inputs = [inputs[0,:].reshape((1,100,100,4)), inputs[1,:].reshape((1,100,100,4))]
            out = self.model.predict(inputs)
            return (out <= threshold)

    def load(self, save_name):
        K.set_session(self.session)
        with self.graph.as_default():
            self.model.load_weights(os.path.join(self.weigths_path, save_name, 'faceID_weights'))
            self.model._make_predict_function()
            print('--- Weights loaded ---')
