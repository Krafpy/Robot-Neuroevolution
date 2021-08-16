#include <EEPROM.h>
#include <Servo.h>

#define SOUND_SPEED 343 // vitesse du son en m/s

// moteurs
Servo motor1;
Servo motor2;
const int pinMotor1 = 10; // PWM
const int pinMotor2 = 11; // PWM

// capteurs à ultrasons
const int trig1 = 2, trig2 = 4, trig3 = 6;
const int echo1 = 3, echo2 = 5, echo3 = 7;

// gestion du temps
const int tempo = 100; // delai entre chaque loop
unsigned long previousMillis = 0;


// distances
const float minDist = 0.03f;
const float maxDist = 0.2f;


// vitesses pour les moteurs
const int motorStop = 90;
const int motor1MaxSpeed = 30;
const int motor2MaxSpeed = 90;
float motor1Speed = 0.f, motor2Speed = 0.f;


// réseau neuronal
const int inputNodes = 3;
const int hiddenNodes = 6;
const int outputNodes = 2;



class Matrix
{
 public:
  int rows, cols, len;
  float *data;

  Matrix(int, int);
  
  void add(Matrix*);
  void product(Matrix*, Matrix*);
  void mapf(float(*f)(float));
  void from_array(float*);
  void slog();
};


Matrix::Matrix(int r, int c){
  this->rows = r;
  this->cols = c;
  this->len = this->rows * this->cols;
  this->data = new float[this->len];

  for(int i = 0; i < this->len; i++){
    this->data[i] = 0.f;
  }
}

void Matrix::add(Matrix *mat){
  if(this->rows == mat->rows && this->cols == mat->cols){
    for(int i = 0; i < this->len; i++){
      this->data[i] += mat->data[i];
    }
  }
}


void Matrix::product(Matrix *mat1, Matrix *mat2){
  if(mat1->cols == mat2->rows){
      
    for(int i = 0; i < this->rows; i++){
      for(int j = 0; j < this->cols; j++){
        float sum = 0;
        for(int k = 0; k < mat1->cols; k++){
          int index1 = k + i * mat1->cols;
          int index2 = j + k * mat2->cols;
          sum += mat1->data[index1] * mat2->data[index2];
        }
        this->data[j + i * this->cols] = sum;
      }
    }
  }
}


void Matrix::mapf(float(*f)(float)){
  for(int i = 0; i < this->len; i++){
    this->data[i] = f(this->data[i]);
  }
}

void Matrix::from_array(float *arr){
  for(int i = 0; i < this->len; i++){
    this->data[i] = arr[i];
  }
}

void Matrix::slog(){
  for(int i = 0; i < this->rows; i++){
    for(int j = 0; j < this->cols; j++){
       Serial.print(this->data[j + i * this->cols]);
       Serial.print("  ");
    }
    Serial.print("\n");
  }
  Serial.print("\n");
}


Matrix *weightsI2H;
Matrix *weightsH2O;
Matrix *biasH;
Matrix *biasO;

Matrix *input;
Matrix *hidden;
Matrix *output;

int total;


float readFloat(unsigned int addr){
  union
  {
    byte b[4];
    float f;
  } data;
  for(int i = 0; i < 4; i++){
   data.b[i] = EEPROM.read(addr+i); 
  }
  return data.f;
}


void writeFloat(unsigned int addr, float x){
  union
  {
    byte b[4];
    float f;
  } data;
  data.f = x;
  for(int i = 0; i < 4; i++){
    EEPROM.write(addr+i, data.b[i]);
  }
}


void download(){ // fonction communiquant avec unity et téléchargeant les nouveaux poids
  unsigned int addr = 1;

  // on récupère les poids dans un array
  while(addr < total * sizeof(float)){
    
    if(Serial.available()){
      
      if(Serial.peek() == 'E'){
        break;
      } else {
        writeFloat(addr, Serial.parseFloat());
        addr += sizeof(float);
      }

      while(Serial.available()){
        Serial.read();
      }
      Serial.print('R');
    }
  }

  //EEPROM.put(1, data); // on sauvegarde l'array de données dans l'EEPROM
}


void load(){ // on charge les poids sauvegardés dans l'EEPROM
  unsigned int addr = 1;
  
  for(int i = 0; i < total; i++){ 
    float val = readFloat(addr);

    //Serial.println(val,6);

    int index = i;

    if(i < weightsI2H->len){
      weightsI2H->data[index] = val;
      
    } else if (i - weightsI2H->len < weightsH2O->len){
      index -= weightsI2H->len;
      weightsH2O->data[index] = val;
      
    } else if (i - weightsI2H->len - weightsH2O->len < biasH->len){
      index -= weightsI2H->len;
      index -= weightsH2O->len;
      biasH->data[index] = val;
      
    } else {
      index -= weightsI2H->len;
      index -= weightsH2O->len;
      index -= biasH->len;
      biasO->data[index] = val;
    }

    addr += sizeof(float);
  }

  //Serial.print('\n');
}


/*int availableMemory() {
  int size = 2048;
  byte *buf;

  while ((buf = (byte *) malloc(--size)) == NULL)
    ;

  free(buf);

  return size;
}*/


void setup() {
  // put your setup code here, to run once:

  // initialisation des servos
  motor1.attach(pinMotor1);
  motor2.attach(pinMotor2);

  // initialisation des pins trig et echo
  pinMode(trig1, OUTPUT);
  pinMode(trig2, OUTPUT);
  pinMode(trig3, OUTPUT);

  pinMode(echo1, INPUT);
  pinMode(echo2, INPUT);
  pinMode(echo3, INPUT); 
  
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, 0);

  while(!Serial);
  Serial.begin(115200); // initialisation de la voie série à 9600 bauds
  
  Serial.flush();
  Serial.print('Y');


  // initialisation du réseau neuronal
  weightsI2H = new Matrix(hiddenNodes, inputNodes);
  weightsH2O = new Matrix(outputNodes, hiddenNodes);
  biasH = new Matrix(hiddenNodes, 1);
  biasO = new Matrix(outputNodes, 1);

  total = weightsI2H->len + weightsH2O->len + biasH->len + biasO->len;

  input = new Matrix(inputNodes, 1);
  hidden = new Matrix(hiddenNodes, 1);
  output = new Matrix(outputNodes, 1);

  // check du premier démarrage
  if(EEPROM.read(0) != 0x00){
    load();
  }
  EEPROM.update(0, 0xFF);
}


float fmap(float x, float in_min, float in_max, float out_min, float out_max)
{
  return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
}


float dist(int trig, int echo){ // fonction permettant la mesure de distance en m
  digitalWrite(trig, HIGH);
  delayMicroseconds(10);
  digitalWrite(trig, LOW);

  unsigned long dt = pulseIn(echo, HIGH, 35000);

  if (dt > 30000) {
    //Serial.println("Onde perdue, mesure echouee !");
    return 1.f;
  } else {
    dt = dt / 2;

    float t = dt / 1000000.0;
    float d = t * SOUND_SPEED;

    d = fmap(d, minDist, maxDist, 0.f, 1.f);
    d = constrain(d, 0.f, 1.f);

    //Serial.print("d (m) = ");
    //Serial.println(d);

    return d;
  }
}


/*float sigmoid(float x){ // fonction d'activation pour les neurones du nn
  return 1.f / (1.f + exp(-x)); 
}*/


float mtanh(float x){ // fonction d'activation pour les neurones du nn
  return (exp(x) - exp(-x)) / (exp(x) + exp(-x));
}


void predict(float d1, float d2, float d3){ // feedforward du nn
  float ins[3] = {d1, d2, d3};
  input->from_array(ins);
  
  // premier niveau du nn
  hidden->product(weightsI2H, input);
  hidden->add(biasH);
  hidden->mapf(mtanh);

  // second niveau du nn
  output->product(weightsH2O, hidden);
  output->add(biasO);
  output->mapf(mtanh);
}



void loop() {
  // put your main code here, to run repeatedly:

  if(Serial.available()){
    if(Serial.read() == 'S'){
      Serial.print('S');
      digitalWrite(LED_BUILTIN, 1);
      download();
      digitalWrite(LED_BUILTIN, 0);
    }
  }

  unsigned long currentMillis = millis();
  
  if(currentMillis - previousMillis > tempo) // une utilise une condition au lieu d'un delay
  {
      
      previousMillis = currentMillis;
  
      // on mesure les distances sur les 3 côtés
      float dr = dist(trig1, echo1);
      float df = dist(trig2, echo2);
      float dl = dist(trig3, echo3);
      // on les affiches dans le port série
      /*Serial.print(dl);
      Serial.print(" | ");
      Serial.print(df);
      Serial.print(" | ");
      Serial.print(dr);
      Serial.print("\n");*/

      // unsigned long startTime = millis(); // variable de debug pour le temps de calcul
      
      predict(dr, df, dl); // on prédit la vitesse des moteurs avec le nn

      //Serial.println(millis() - startTime); // on affiche le temps mis par l'opération

      motor1Speed += output->data[0];
      motor2Speed += output->data[1];
      motor1Speed = constrain(motor1Speed, 0.f, 1.f);
      motor2Speed = constrain(motor2Speed, 0.f, 1.f);

      /*Serial.print(motor2Speed);
      Serial.print(" | ");
      Serial.print(motor1Speed);
      Serial.print("\n");*/
    
      // vitesse max pour les deux moteurs avec asservissement (v = 0 à 90)
      motor1.write((float)motorStop + (float)motor1Speed * (float)motor1MaxSpeed);
      motor2.write((float)motorStop - (float)motor2Speed * (float)motor2MaxSpeed);

      /*Serial.print("Av. SRAM: ");
      Serial.print(availableMemory());
      
      Serial.print("\n\n");*/
  }
}
