using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetwork {

	public int inputNodes, hiddenNodes, outputNodes;
	
	public Matrix weightsI2H, weightsH2O;
	public Matrix biasH, biasO;

	public NeuralNetwork(int inputs, int hidden, int outputs){
		inputNodes = inputs;
		hiddenNodes = hidden;
		outputNodes = outputs;

		weightsI2H = new Matrix(hiddenNodes, inputNodes);
		weightsH2O = new Matrix(outputNodes, hiddenNodes);
		weightsI2H.Randomize();
		weightsH2O.Randomize();

		biasH = new Matrix(hiddenNodes, 1);
		biasO = new Matrix(outputNodes, 1);
		biasH.Randomize();
		biasO.Randomize();
	}

	public float[,] Predict(float[,] inputArray){ // feedforward
		Matrix inputs = Matrix.fromArray(inputArray);
		
		// On fait passer les données à travers premier niveau
		Matrix hidden = Matrix.Product(weightsI2H, inputs);
		hidden.Add(biasH);
		hidden.Map(TanH);
		
		// On fait passer les données à travers le second niveau
		Matrix output = Matrix.Product(weightsH2O, hidden);
		output.Add(biasO);
		output.Map(TanH);

		return output.toArray();
	}

	float Sigmoid(float x){
		return 1f / (1f + Mathf.Exp(-x));
	}

	float TanH(float x){
		return (Mathf.Exp(x) - Mathf.Exp(-x)) / (Mathf.Exp(x) + Mathf.Exp(-x));
	}

	public static NeuralNetwork Crossover(NeuralNetwork nn1, NeuralNetwork nn2){
		if(nn1.inputNodes == nn2.inputNodes && nn1.hiddenNodes == nn2.hiddenNodes && nn1.outputNodes == nn2.outputNodes){
			
			NeuralNetwork nn = new NeuralNetwork(nn1.inputNodes, nn1.hiddenNodes, nn1.outputNodes);

			nn.weightsI2H = Matrix.Crossover(nn1.weightsI2H, nn2.weightsI2H);
			nn.weightsH2O = Matrix.Crossover(nn1.weightsH2O, nn2.weightsH2O);
			nn.biasH = Matrix.Crossover(nn1.biasH, nn2.biasH);
			nn.biasO = Matrix.Crossover(nn1.biasO, nn2.biasO);

			return nn;

		} else {
			return null;
		}
	}

	public void Mutate(float mutationRate){
		weightsI2H.Mutate(mutationRate);
		weightsH2O.Mutate(mutationRate);
		biasH.Mutate(mutationRate);
		biasO.Mutate(mutationRate);
	}

	public float[] toDataArray(){
		float[] w_I2H = weightsI2H.toArray1D();
		float[] w_H2O = weightsH2O.toArray1D();
		float[] bH = biasH.toArray1D();
		float[] bO = biasO.toArray1D();

		float[] data = new float[w_I2H.Length + w_H2O.Length + bH.Length + bO.Length];

		for(int i = 0; i < data.Length; i++){
			int index = i;

			if(i < w_I2H.Length){
				data[i] = w_I2H[index];
			
			} else if (i - w_I2H.Length < w_H2O.Length){
				index -= w_I2H.Length;
				data[i] = w_H2O[index];
			
			} else if (i - w_I2H.Length - w_H2O.Length < bH.Length){
				index -= w_I2H.Length;
				index -= w_H2O.Length;
				data[i] = bH[index];
			
			} else {
				index -= w_I2H.Length;
				index -= w_H2O.Length;
				index -= bH.Length;
				data[i] = bO[index];
			}
		}

		return data;
	}

}