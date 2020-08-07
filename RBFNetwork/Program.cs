using System;
using System.Linq;
using Common.Utility;

namespace RBFNetwork
{
    internal class RadialTrainProgram
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("\nBegin radial basis function (RBF) network training\n");
            Console.WriteLine("Goal is to train an RBF network on iris flower data to predict");
            Console.WriteLine("species from sepal length and width, and petal length and width.");

            var inputData = DataManager.ReadInputData("absenteeism at workSel.csv", null);
            ////List<string> s = DataManager.ReadColumnNames("absenteeism at work.csv");

            ////s.RemoveAt(s.Count - 1);
            ////s.AddRange(new []
            ////{
            ////    "Certain infectious and parasitic diseases",
            ////    "Neoplasms",
            ////    "Diseases of the blood and blood-forming organs and certain disorders involving the immune mechanism  ",
            ////    "Endocrine, nutritional and metabolic diseases",
            ////    "Mental and behavioural disorders",
            ////    "Diseases of the nervous system",
            ////    "Diseases of the eye and adnexa",
            ////    "Diseases of the ear and mastoid process",
            ////    "Diseases of the circulatory system",
            ////    "Diseases of the respiratory system",
            ////    "Diseases of the digestive system",
            ////    "Diseases of the skin and subcutaneous tissue",
            ////    "Diseases of the musculoskeletal system and connective tissue",
            ////    "Diseases of the genitourinary system",
            ////    "Pregnancy, childbirth and the puerperium",
            ////    "Certain conditions originating in the perinatal period",
            ////    "Congenital malformations, deformations and chromosomal abnormalities",
            ////    "Symptoms, signs and abnormal clinical and laboratory findings, not elsewhere classified",
            ////    "Injury, poisoning and certain other consequences of external causes",
            ////    "External causes of morbidity and mortality",
            ////    "Factors influencing health status and contact with health services",
            ////    "patient follow-up",
            ////    "medical consultation",
            ////    "blood donation",
            ////    "laboratory examination",
            ////    "unjustified absence",
            ////    "physiotherapy",
            ////    "dental consultation"
            ////});
            ////inputData.ForEach(id =>
            ////{
            ////    for (int i = 1; i <= 28; i++)
            ////    {
            ////        id.Inputs.Add(i == id.Result ? 1 : 0);
            ////    }
            ////});
            ////DataManager.WriteData("res.csv", s.ToArray(), inputData);

            var allData = inputData.Select(c => c.Inputs.Select(i => i).ToArray()).ToArray();

            Console.WriteLine("\nFirst four and last line of normalized, encoded input data:\n");

            Console.WriteLine("\nSplitting data into 80%-20% train and test sets");
            double[][] trainData = null;
            double[][] testData = null;
            var seed = 8;
            GetTrainTest(allData, seed, out trainData, out testData); // 80-20 hold-out 

            Console.WriteLine("\nCreating a 9-6-28 radial basis function network");
            var numInput = 9;
            var numHidden = 6;
            var numOutput = 28;
            var rn = new RadialNetwork(numInput, numHidden, numOutput);

            Console.WriteLine("\nBeginning RBF training\n");
            var maxIterations = 200;
            var bestWeights = rn.Train(trainData, maxIterations);

            Console.WriteLine("\nEvaluating result RBF classification accuracy on the test data");
            rn.SetWeights(bestWeights);

            var s = DataManager.ReadColumnNames("absenteeism at workSel.csv");

            inputData.ForEach(id =>
            {
                var outputs = rn.ComputeOutputs(id.Inputs.Select(c => c).Take(9).ToArray());

                for (var i = 0; i < 28; i++) id.Inputs[i + id.Inputs.Count - 28] = outputs[i];
            });

            DataManager.WriteData("res.csv", s.ToArray(), inputData);

            Console.WriteLine("\nEnd RBF network training\n");
            Console.ReadLine();
        } // Main()

        private static void GetTrainTest(double[][] allData, int seed, out double[][] trainData,
            out double[][] testData)
        {
            // 80-20 hold-out validation
            var allIndices = new int[allData.Length];
            for (var i = 0; i < allIndices.Length; ++i)
                allIndices[i] = i;

            ////Random rnd = new Random(seed);
            ////for (int i = 0; i < allIndices.Length; ++i) // shuffle indices
            ////{
            ////    int r = rnd.Next(i, allIndices.Length);
            ////    int tmp = allIndices[r];
            ////    allIndices[r] = allIndices[i];
            ////    allIndices[i] = tmp;
            ////}

            var numTrain = (int) (0.80 * allData.Length);
            var numTest = allData.Length - numTrain;

            trainData = new double[numTrain][];
            testData = new double[numTest][];

            var j = 0;
            for (var i = 0; i < numTrain; ++i)
                trainData[i] = allData[allIndices[j++]];
            for (var i = 0; i < numTest; ++i)
                testData[i] = allData[allIndices[j++]];
        }
    }


    public class RadialNetwork
    {
        private static Random rnd;
        private readonly double[][] centroids;
        private readonly double[][] hoWeights;
        private readonly double[] inputs;
        private readonly int numHidden;
        private readonly int numInput;
        private readonly int numOutput;
        private readonly double[] oBiases;
        private readonly double[] outputs;
        private readonly double[] widths;

        public RadialNetwork(int numInput, int numHidden, int numOutput)
        {
            rnd = new Random(0);
            this.numInput = numInput;
            this.numHidden = numHidden;
            this.numOutput = numOutput;
            inputs = new double[numInput];
            centroids = MakeMatrix(numHidden, numInput);
            widths = new double[numHidden];
            hoWeights = MakeMatrix(numHidden, numOutput);
            oBiases = new double[numOutput];
            outputs = new double[numOutput];
        } // ctor

        private static double[][] MakeMatrix(int rows, int cols) // helper for ctor
        {
            var result = new double[rows][];
            for (var r = 0; r < rows; ++r)
                result[r] = new double[cols];
            return result;
        }

        // -- methods related to getting and setting centroids, widths, weights, bias values ----------

        public void SetWeights(double[] weights)
        {
            // this.hoWeights has numHidden row and numOutput cols
            // this.oBiases has numOutput values
            if (weights.Length != numHidden * numOutput + numOutput)
                throw new Exception("Bad weights length in SetWeights");
            var k = 0; // ptr into weights
            for (var i = 0; i < numHidden; ++i)
            for (var j = 0; j < numOutput; ++j)
                hoWeights[i][j] = weights[k++];
            for (var i = 0; i < numOutput; ++i)
                oBiases[i] = weights[k++];
        }

        public double[] GetWeights()
        {
            var result = new double[numHidden * numOutput + numOutput];
            var k = 0;
            for (var i = 0; i < numHidden; ++i)
            for (var j = 0; j < numOutput; ++j)
                result[k++] = hoWeights[i][j];
            for (var i = 0; i < numOutput; ++i)
                result[k++] = oBiases[i];
            return result;
        }

        // put GetAllWeights() and SetAllWeights() here to fetch and set
        // centroid, width, weight, and bias values as a group

        // -- methods related to training error and test classification accuracy ----------------------

        private double MeanSquaredError(double[][] trainData, double[] weights)
        {
            // assumes that centroids and widths have been set!
            SetWeights(weights); // copy the weights to valuate in

            var xValues = new double[numInput]; // inputs
            var tValues = new double[numOutput]; // targets
            var sumSquaredError = 0.0;
            for (var i = 0; i < trainData.Length; ++i) // walk through each trainingb data item
            {
                // following assumes data has all x-values first, followed by y-values!
                Array.Copy(trainData[i], xValues, numInput); // extract inputs
                Array.Copy(trainData[i], numInput, tValues, 0, numOutput); // extract targets
                var yValues =
                    ComputeOutputs(xValues); // compute the outputs using centroids, widths, weights, bias values
                for (var j = 0; j < yValues.Length; ++j)
                    sumSquaredError += (yValues[j] - tValues[j]) * (yValues[j] - tValues[j]);
            }

            return sumSquaredError / trainData.Length;
        }

        // consider MeanCrossEntropy() here as an alternative to MSE

        public double Accuracy(double[][] testData)
        {
            // percentage correct using winner-takes all
            var numCorrect = 0;
            var numWrong = 0;
            var xValues = new double[numInput]; // inputs
            var tValues = new double[numOutput]; // targets
            double[] yValues; // computed Y

            for (var i = 0; i < testData.Length; ++i)
            {
                Array.Copy(testData[i], xValues, numInput); // parse test data into x-values and t-values
                Array.Copy(testData[i], numInput, tValues, 0, numOutput);
                yValues = ComputeOutputs(xValues);
                var maxIndex = MaxIndex(yValues); // which cell in yValues has largest value?
                if (tValues[maxIndex] == 1.0) // ugly. consider AreEqual(double x, double y)
                    ++numCorrect;
                else
                    ++numWrong;
            }

            return numCorrect * 1.0 / (numCorrect + numWrong); // ugly 2 - check for divide by zero
        }

        private static int MaxIndex(double[] vector) // helper for Accuracy()
        {
            // index of largest value
            var bigIndex = 0;
            var biggestVal = vector[0];
            for (var i = 0; i < vector.Length; ++i)
                if (vector[i] > biggestVal)
                {
                    biggestVal = vector[i];
                    bigIndex = i;
                }

            return bigIndex;
        }

        // -- methods related to RBF network input-output mechanism -----------------------------------

        public double[] ComputeOutputs(double[] xValues)
        {
            // use centroids, widths, weights and input xValues to compute, store, and return numOutputs output values
            Array.Copy(xValues, inputs, xValues.Length); // place data inputs into RBF net inputs

            var hOutputs = new double[numHidden]; // hidden node outputs
            for (var j = 0; j < numHidden; ++j) // each hidden node
            {
                var d = EuclideanDist(inputs, centroids[j], inputs.Length); // could use a 'distSquared' approach
                //Console.WriteLine("\nHidden[" + j + "] distance = " + d.ToString("F4"));
                var r = -1.0 * (d * d) / (2 * widths[j] * widths[j]);
                var g = Math.Exp(r);
                //Console.WriteLine("Hidden[" + j + "] output = " + g.ToString("F4"));
                hOutputs[j] = g;
            }

            var tempResults = new double[numOutput];

            for (var k = 0; k < numOutput; ++k)
            for (var j = 0; j < numHidden; ++j)
                tempResults[k] += hOutputs[j] * hoWeights[j][k]; // accumulate

            for (var k = 0; k < numOutput; ++k)
                tempResults[k] += oBiases[k]; // add biases

            var finalOutputs = Softmax(tempResults); // scale the raw output so values sum to 1.0

            //Console.WriteLine("outputs:");
            //Helpers.ShowVector(finalOutputs, 3, finalOutputs.Length, true);
            //Console.ReadLine();

            Array.Copy(finalOutputs, outputs, finalOutputs.Length); // transfer computed outputs to RBF net outputs

            var returnResult = new double[numOutput]; // also return computed outputs for convenience
            Array.Copy(finalOutputs, returnResult, outputs.Length);
            return returnResult;
        } // ComputeOutputs


        private static double[] Softmax(double[] rawOutputs)
        {
            // helper for ComputeOutputs
            // does all output nodes at once so scale doesn't have to be re-computed each time
            // determine max output sum
            var max = rawOutputs[0];
            for (var i = 0; i < rawOutputs.Length; ++i)
                if (rawOutputs[i] > max)
                    max = rawOutputs[i];

            // determine scaling factor -- sum of exp(each val - max)
            var scale = 0.0;
            for (var i = 0; i < rawOutputs.Length; ++i)
                scale += Math.Exp(rawOutputs[i] - max);

            var result = new double[rawOutputs.Length];
            for (var i = 0; i < rawOutputs.Length; ++i)
                result[i] = Math.Exp(rawOutputs[i] - max) / scale;

            return result; // now scaled so that all values sum to 1.0
        }

        // -- methods related to training: DoCentroids, DistinctIndices, AvgAbsDist, DoWidths,
        //    DoWeights, Train, Shuffle ----------------------------------------------------------

        private void DoCentroids(double[][] trainData)
        {
            // centroids are representative inputs that are relatively different
            // compute centroids using the x-vaue of training data
            // store into this.centroids
            var numAttempts = trainData.Length;
            var goodIndices = new int[numHidden]; // need one centroid for each hidden node
            var maxAvgDistance = double.MinValue; // largest average distance for a set of candidate indices
            for (var i = 0; i < numAttempts; ++i)
            {
                var randomIndices = DistinctIndices(numHidden, trainData.Length); // candidate indices
                var sumDists = 0.0; // sum of distances between adjacent candidates (not all candiates)
                for (var j = 0; j < randomIndices.Length - 1; ++j) // adjacent pairs only
                {
                    var firstIndex = randomIndices[j];
                    var secondIndex = randomIndices[j + 1];
                    sumDists += AvgAbsDist(trainData[firstIndex], trainData[secondIndex],
                        numInput); // just the input terms
                }

                var estAvgDist = sumDists / numInput; // estimated average distance for curr candidates
                if (estAvgDist > maxAvgDistance) // curr candidates are far apart
                {
                    maxAvgDistance = estAvgDist;
                    Array.Copy(randomIndices, goodIndices, randomIndices.Length); // save curr candidates
                }
            } // now try a new set of candidates

            Console.WriteLine("The indices (into training data) of the centroids are:");
            Helpers.ShowVector(goodIndices, goodIndices.Length, true);

            // store copies of x-vales of data pointed to by good indices into this.centroids
            for (var i = 0; i < numHidden; ++i)
            {
                var idx = goodIndices[i]; // idx points to trainData
                for (var j = 0; j < numInput; ++j) centroids[i][j] = trainData[idx][j]; // make a copy of values
            }
        } // DoCentroids

        private static double AvgAbsDist(double[] v1, double[] v2, int numTerms)
        {
            // average absolute difference distance between two vectors, first numTerms only
            // helper for computing centroids
            if (v1.Length != v2.Length)
                throw new Exception("Vector lengths not equal in AvgAbsDist()");
            var sum = 0.0;
            for (var i = 0; i < numTerms; ++i)
            {
                var delta = Math.Abs(v1[i] - v2[i]);
                sum += delta;
            }

            return sum / numTerms;
        }

        private int[] DistinctIndices(int n, int range)
        {
            // helper for ComputeCentroids()
            // generate n distinct numbers in [0, range-1] using reservoir sampling
            // assumes rnd exists
            var result = new int[n];
            for (var i = 0; i < n; ++i)
                result[i] = i;

            for (var t = n; t < range; ++t)
            {
                var m = rnd.Next(0, t + 1);
                if (m < n) result[m] = t;
            }

            return result;
        }

        private void DoWidths(double[][] centroids)
        {
            // compute widths based on centroids, store into this.widths
            // note the centroids parameter could be omitted - the intent is to make relationship clear
            // this version uses a common width which is the average dist between all centroids
            var sumOfDists = 0.0;
            var ct = 0; // could calculate number pairs instead
            for (var i = 0; i < centroids.Length - 1; ++i)
            for (var j = i + 1; j < centroids.Length; ++j)
            {
                var dist = EuclideanDist(centroids[i], centroids[j], centroids[i].Length);
                sumOfDists += dist;
                ++ct;
            }

            var avgDist = sumOfDists / ct;
            var width = avgDist;

            Console.WriteLine("The common width is: " + width.ToString("F4"));

            for (var i = 0; i < widths.Length; ++i) // all widths the same
                widths[i] = width;
        }

        private double[] DoWeights(double[][] trainData, int maxIterations)
        {
            // use PSO to find weights and bias values that produce a RBF network
            // that best matches training data
            var numberParticles = trainData.Length / 3;

            var Dim = numHidden * numOutput + numOutput; // dimensions is num weights + num biases
            var minX = -10.0; // implicitly assumes data has been normalizzed
            var maxX = 10.0;
            var minV = minX;
            var maxV = maxX;
            var swarm = new Particle[numberParticles];
            var bestGlobalPosition =
                new double[Dim]; // best solution found by any particle in the swarm. implicit initialization to all 0.0
            var smallesttGlobalError = double.MaxValue; // smaller values better

            // initialize swarm
            for (var i = 0; i < swarm.Length; ++i) // initialize each Particle in the swarm
            {
                var randomPosition = new double[Dim];
                for (var j = 0; j < randomPosition.Length; ++j)
                {
                    var lo = minX;
                    var hi = maxX;
                    randomPosition[j] = (hi - lo) * rnd.NextDouble() + lo; // 
                }

                var err = MeanSquaredError(trainData,
                    randomPosition); // error associated with the random position/solution
                var randomVelocity = new double[Dim];

                for (var j = 0; j < randomVelocity.Length; ++j)
                {
                    var lo = -1.0 * Math.Abs(maxV - minV);
                    var hi = Math.Abs(maxV - minV);
                    randomVelocity[j] = (hi - lo) * rnd.NextDouble() + lo;
                }

                swarm[i] = new Particle(randomPosition, err, randomVelocity, randomPosition, err);

                // does current Particle have global best position/solution?
                if (swarm[i].error < smallesttGlobalError)
                {
                    smallesttGlobalError = swarm[i].error;
                    swarm[i].position.CopyTo(bestGlobalPosition, 0);
                }
            } // initialization

            // main PSO algorithm
            // compute new velocity -> compute new position -> check if new best

            var w = 0.729; // inertia weight
            var c1 = 1.49445; // cognitive/local weight
            var c2 = 1.49445; // social/global weight
            double r1, r2; // cognitive and social randomizations

            var sequence = new int[numberParticles]; // process particles in random order
            for (var i = 0; i < sequence.Length; ++i)
                sequence[i] = i;

            var iteration = 0;
            while (iteration < maxIterations)
            {
                if (smallesttGlobalError < 0.060) break; // early exit (MSE)

                var newVelocity = new double[Dim]; // step 1
                var newPosition = new double[Dim]; // step 2
                double newError; // step 3

                Shuffle(sequence); // move particles in random sequence

                for (var pi = 0; pi < swarm.Length; ++pi) // each Particle (index)
                {
                    var i = sequence[pi];
                    var currP = swarm[i]; // for coding convenience

                    // 1. compute new velocity
                    for (var j = 0; j < currP.velocity.Length; ++j) // each x value of the velocity
                    {
                        r1 = rnd.NextDouble();
                        r2 = rnd.NextDouble();

                        // velocity depends on old velocity, best position of parrticle, and 
                        // best position of any particle
                        newVelocity[j] = w * currP.velocity[j] +
                                         c1 * r1 * (currP.bestPosition[j] - currP.position[j]) +
                                         c2 * r2 * (bestGlobalPosition[j] - currP.position[j]);

                        if (newVelocity[j] < minV)
                            newVelocity[j] = minV;
                        else if (newVelocity[j] > maxV)
                            newVelocity[j] = maxV; // crude way to keep velocity in range
                    }

                    newVelocity.CopyTo(currP.velocity, 0);

                    // 2. use new velocity to compute new position
                    for (var j = 0; j < currP.position.Length; ++j)
                    {
                        newPosition[j] = currP.position[j] + newVelocity[j]; // compute new position
                        if (newPosition[j] < minX)
                            newPosition[j] = minX;
                        else if (newPosition[j] > maxX)
                            newPosition[j] = maxX;
                    }

                    newPosition.CopyTo(currP.position, 0);

                    // 3. use new position to compute new error
                    // consider cross-entropy error instead of MSE
                    newError = MeanSquaredError(trainData, newPosition); // makes next check a bit cleaner
                    currP.error = newError;

                    if (newError < currP.smallestError) // new particle best?
                    {
                        newPosition.CopyTo(currP.bestPosition, 0);
                        currP.smallestError = newError;
                    }

                    if (newError < smallesttGlobalError) // new global best?
                    {
                        newPosition.CopyTo(bestGlobalPosition, 0);
                        smallesttGlobalError = newError;
                    }

                    // consider using weight decay, particle death here
                } // each Particle

                ++iteration;
            } // while (main PSO processing loop)

            //Console.WriteLine("\n\nFinal training MSE = " + smallesttGlobalError.ToString("F4") + "\n\n");

            // copy best weights found into RBF network, and also return them
            SetWeights(bestGlobalPosition);
            var returnResult = new double[numHidden * numOutput + numOutput];
            Array.Copy(bestGlobalPosition, returnResult, bestGlobalPosition.Length);

            Console.WriteLine("The best weights and bias values found are:\n");
            Helpers.ShowVector(bestGlobalPosition, 3, 10, true);
            return returnResult;
        } // DoWeights

        private static void Shuffle(int[] sequence)
        {
            // helper for DoWeights to process particles in random order
            for (var i = 0; i < sequence.Length; ++i)
            {
                var r = rnd.Next(i, sequence.Length);
                var tmp = sequence[r];
                sequence[r] = sequence[i];
                sequence[i] = tmp;
            }
        }

        public double[] Train(double[][] trainData, int maxIterations)
        {
            Console.WriteLine("\n1. Computing " + numHidden + " centroids");
            DoCentroids(trainData); // find representative data, store their x-values into this.centroids

            Console.WriteLine("\n2. Computing a common width for each hidden node");
            DoWidths(centroids); // measure of how far apart centroids are

            var numWts = numHidden * numOutput + numOutput;
            Console.WriteLine("\n3. Determining " + numWts + " weights and bias values using PSO algorithm");
            var bestWeights =
                DoWeights(trainData,
                    maxIterations); // use PSO to find weights that best (lowest MSE) weights and biases

            return bestWeights;
        } // Train

        // -- The Euclidean Distance function is used by RBF ComputeOutputs and also DoWidths ----

        private static double EuclideanDist(double[] v1, double[] v2, int numTerms)
        {
            // Euclidean distance between two vectors, first numTerms only
            // helper for computing RBF outputs and computing hidden node widths
            if (v1.Length != v2.Length)
                throw new Exception("Vector lengths not equal in EuclideanDist()");
            var sum = 0.0;
            for (var i = 0; i < numTerms; ++i)
            {
                var delta = (v1[i] - v2[i]) * (v1[i] - v2[i]);
                sum += delta;
            }

            return Math.Sqrt(sum);
        }
    }

    public class Particle
    {
        public double[] bestPosition; // best position found so far by this Particle
        public double error; // error so smaller is better
        public double[] position; // equivalent to x-Values and/or solution
        public double smallestError;
        public double[] velocity;

        public Particle(double[] position, double error, double[] velocity, double[] bestPosition, double smallestError)
        {
            this.position = new double[position.Length];
            position.CopyTo(this.position, 0);
            this.error = error;
            this.velocity = new double[velocity.Length];
            velocity.CopyTo(this.velocity, 0);
            this.bestPosition = new double[bestPosition.Length];
            bestPosition.CopyTo(this.bestPosition, 0);
            this.smallestError = smallestError;
        }

    }

    public class Helpers
    {
        public static double[][] MakeMatrix(int rows, int cols)
        {
            var result = new double[rows][];
            for (var i = 0; i < rows; ++i)
                result[i] = new double[cols];
            return result;
        }

        public static void ShowVector(double[] vector, int decimals, int valsPerLine, bool blankLine)
        {
            for (var i = 0; i < vector.Length; ++i)
            {
                if (i > 0 && i % valsPerLine == 0) // max of 12 values per row 
                    Console.WriteLine("");
                if (vector[i] >= 0.0) Console.Write(" ");
                Console.Write(vector[i].ToString("F" + decimals) + " "); // n decimals
            }

            if (blankLine) Console.WriteLine("\n");
        }

        public static void ShowVector(int[] vector, int valsPerLine, bool blankLine)
        {
            for (var i = 0; i < vector.Length; ++i)
            {
                if (i > 0 && i % valsPerLine == 0) // max of 12 values per row 
                    Console.WriteLine("");
                if (vector[i] >= 0.0) Console.Write(" ");
                Console.Write(vector[i] + " ");
            }

            if (blankLine) Console.WriteLine("\n");
        }

        public static void ShowMatrix(double[][] matrix, int numRows, int decimals, bool lineNumbering,
            bool showLastLine)
        {
            var ct = 0;
            if (numRows == -1) numRows = int.MaxValue; // if numRows == -1, show all rows
            for (var i = 0; i < matrix.Length && ct < numRows; ++i)
            {
                if (lineNumbering)
                    Console.Write(i.ToString().PadLeft(3) + ": ");
                for (var j = 0; j < matrix[0].Length; ++j)
                {
                    if (matrix[i][j] >= 0.0) Console.Write(" "); // blank space instead of '+' sign
                    Console.Write(matrix[i][j].ToString("F" + decimals) + " ");
                }

                Console.WriteLine("");
                ++ct;
            }

            if (showLastLine && numRows < matrix.Length)
            {
                Console.WriteLine("      ........\n ");
                var i = matrix.Length - 1;
                Console.Write(i.ToString().PadLeft(3) + ": ");
                for (var j = 0; j < matrix[0].Length; ++j)
                {
                    if (matrix[i][j] >= 0.0) Console.Write(" "); // blank space instead of '+' sign
                    Console.Write(matrix[i][j].ToString("F" + decimals) + " ");
                }
            }

            Console.WriteLine("");
        }
    }

}