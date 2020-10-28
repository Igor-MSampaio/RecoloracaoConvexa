using Gurobi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RecoloracaoConvexa
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var instancia = "rand_10_7";
                StreamReader file = File.OpenText(@"Instancias\" + instancia + ".txt");
                string[] words = file.ReadLine().Split(' ');
                int nVertices = int.Parse(words[0]);
                int nCores = int.Parse(words[1]);

                List<int> caminhoColorido = new List<int>();

                string line;

                while ((line = file.ReadLine()) != null)
                {
                    caminhoColorido.Add(int.Parse(line));
                }


                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Criação e inicialização do ambiente.
                GRBEnv env = new GRBEnv(true);
                env.Set("LogFile", "Recoloracao.log");
                env.Set(GRB.IntParam.LogToConsole, 0);
                env.Start();

                // Criação do modelo vazio a partir do ambiente.
                GRBModel model = new GRBModel(env);
                model.ModelName = "Recoloracao";
                model.Parameters.TimeLimit = 1800.00;

                // Variáveis do modelo.

                // Variáveis referente aos vértices do grafo e suas cores.
                GRBVar[,] X = new GRBVar[nVertices, nCores];
                var obj = new GRBLinExpr();

                for (int i = 0; i < nVertices; i++)
                {
                    for (int j = 0; j < nCores; j++)
                    {
                        string nome = "X_ " + i.ToString() + "_c " + j.ToString();

                        X[i, j] = model.AddVar(0.0, 1.0, 1.0, GRB.BINARY, nome);

                        if (caminhoColorido[i] != j + 1)
                            obj.AddTerm(1, X[i, j]);
                    }
                }

                model.SetObjective(obj, GRB.MINIMIZE);

                // Restrição 1
                for (int i = 0; i < nVertices; i++)
                {
                    GRBLinExpr soma = new GRBLinExpr();

                    for (int c = 0; c < nCores; c++)
                        soma.AddTerm(1.0, X[i, c]);

                    model.AddConstr(soma, GRB.EQUAL, 1, "Cor vértice" + i);
                }

                // Restrição 2
                for (int p = 0; p < nVertices - 2; p++)
                {
                    for (int r = p + 2; r < nVertices; r++)
                    {
                        for (int q = p + 1; q < r; q++)
                        {
                            for (int k = 0; k < nCores; k++)
                            {
                                model.AddConstr(X[p, k] - X[q, k] + X[r, k], GRB.LESS_EQUAL, 1, "p" + p + "-q" + q + "-r" + r + "-k" + k);
                            }
                        }
                    }
                }

                model.Optimize();

                stopwatch.Stop();

                Console.WriteLine("----------------------------------------------------------------------------------------");
                Console.WriteLine($"Número de variáveis = {model.NumVars}");
                Console.WriteLine($"Número de restrições lineares = {model.NumConstrs}");
                Console.WriteLine($"Número de coeficientes diferentes de zero na matriz de restrição = {model.NumNZs}");
                Console.WriteLine($"Valor objetivo para a solução atual: {model.ObjVal}");
                Console.WriteLine($"Tempo de execução para a otimização mais recente: {model.Runtime}");
                Console.WriteLine($"Número de nós de ramificação e corte explorados na otimização mais recente: {model.NodeCount}");
                Console.WriteLine($"Tempo total: {stopwatch.Elapsed}");
                Console.WriteLine("----------------------------------------------------------------------------------------");

                model.Dispose();
                env.Dispose();
            }
            catch (GRBException e)
            {
                Console.WriteLine("Código do erro: " + e.ErrorCode + ". " + e.Message + " - " + DateTime.Now);
                throw;
            }
        }
    }
}
