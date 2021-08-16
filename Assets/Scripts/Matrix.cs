using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Matrix {

	public static float std;
    
    public int rows, cols;
	public float[,] data;

	public Matrix(int r, int c){ // constructeur
		rows = r;
		cols = c;
		data = new float[rows, cols];

		for(int i = 0; i < rows; i++){
			for(int j = 0; j < cols; j++){
				data[i, j] = 0f;
			}
		}
	}


	public static Matrix fromArray(float[,] array){ // permet de créer une matrice à partir d'une liste
		int r = array.GetLength(0);
		int c = array.GetLength(1);
		Matrix mat = new Matrix(r, c);

		for(int i = 0; i < r; i++){
			for(int j = 0; j < c; j++){
				mat.data[i, j] = array[i, j];
			}
		}

		return mat;
	}

    public float[,] toArray(){ // renvoit la matrice convertit un array correspondant (= data)
		float[,] array = new float[rows, cols];

		for(int i = 0; i < rows; i++){
			for(int j = 0; j < cols; j++){
				array[i, j] = data[i, j];
			}
		}

		return array;
	}

    public float[] toArray1D(){
        float[] array = new float[rows * cols];

		/*Debug.Log(rows);
        Debug.Log(cols);
        Debug.Log(array.Length);
        Debug.Log("---");*/
        for(int i = 0; i < rows; i++){
			for(int j = 0; j < cols; j++){
				array[j + i * cols] = data[i, j];
			}
		}

        return array;
    }

	public void Randomize(){
		for(int i = 0; i < rows; i++){
			for(int j = 0; j < cols; j++){
				data[i, j] = UnityEngine.Random.Range(-1f, 1f); // évite l'ambiguité avec le Random de System
			}
		}
	}

	public void Map(Func<float, float> f){ // applique une fonction sur chaque éléments de la matrice
		for(int i = 0; i < rows; i++){
			for(int j = 0; j < cols; j++){
				data[i, j] = f(data[i, j]);
			}
		}
	}

    public void Add(Matrix mat)
    {
        if (rows == mat.rows)
        {
            if (cols == mat.cols)
            {
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        data[i, j] += mat.data[i, j];
                    }
                }
            }
            else
            {
                Debug.Log("The matrices don't have the same number of cols");
            }
        }
        else
        {
            Debug.Log("The matrices don't have the same number of rows");
        }
    }

    public static Matrix Product(Matrix mat1, Matrix mat2)
    {
        if (mat1.cols != mat2.rows)
        {
            Debug.Log("The number of cols of mat1 must match the number of rows of mat2");

			return null;
        }
        else
        {
            Matrix matResult = new Matrix(mat1.rows, mat2.cols);

            for (int i = 0; i < matResult.rows; i++)
            {
                for (int j = 0; j < matResult.cols; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < mat1.cols; k++)
                    {
                        sum += mat1.data[i,k] * mat2.data[k,j];
                    }
                    matResult.data[i,j] = sum;
                }
            }

			return matResult;
        }
    }

    public static Matrix Crossover(Matrix mat1, Matrix mat2)
    {
        if (mat1.rows == mat2.rows && mat1.cols == mat2.cols)
        {
            Matrix matResult = new Matrix(mat1.rows, mat1.cols);

            for (int i = 0; i < matResult.rows; i++)
            {
                for (int j = 0; j < matResult.cols; j++)
                {
                    if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f)
                    {
                        matResult.data[i, j] = mat1.data[i, j];
                    }
                    else
                    {
                        matResult.data[i, j] = mat2.data[i, j];
                    }
                }
            }
            return matResult;
        }
        else
        {
            Debug.Log("The size of mat1 must match the size of mat2");

            return null;
        }
    }

    public void Mutate(float mutRate)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (UnityEngine.Random.Range(0.0f, 1.0f) < mutRate)
                {
                    data[i, j] += NextGaussian();
                }
            }
        }
    }

    public void Print(){
		string text = "";

		for(int i = 0; i < rows; i++){
			for(int j = 0; j < cols; j++){
				text += data[i, j];
				if(j < cols - 1){
					text += "   ";
				}
			}
			text += "\n\n";
		}

		Debug.Log(text);
	}


	float NextGaussian() {
		float v1, v2, s;
		int hack = 0;
		do {
			
			do {
				v1 = 2.0f * UnityEngine.Random.Range(0f,1f) - 1.0f;
				v2 = 2.0f * UnityEngine.Random.Range(0f,1f) - 1.0f;
				s = v1 * v1 + v2 * v2;

				hack++;
				if(hack > 100){
					return 0f;
				}
			} while (s >= 1.0f || s == 0f);
			
			s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

			s = v1 * s;

		} while(s < -1f || s > 1f);
	
		return s * std;
	}

	/*float NormalRandom(){
		float v1 = UnityEngine.Random.value;
		float v2 = UnityEngine.Random.value;

		float n = Mathf.Sqrt(-2.0f * Mathf.Log(v1)) * Mathf.Cos((2.0f * Mathf.PI) * v2);

		n = Interpolate(n, -4f, 4f, -1f, 1f);

		return Mathf.Clamp(n, -1f, 1f);
	}

	float Interpolate(float x, float in_min, float in_max, float out_min, float out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }*/
}