using Gurobi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RecoloracaoConvexa
{
    class Program
    {
		public static double epsilon = 0.00000001;
		public static int n_BB_nodes = 0;

		public static void Main2(string[] args)
        {
            try
            {
                var instancia = "rand_20_7";
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

                // Variáveis do modelo.

                // Variáveis referente aos vértices do grafo e suas cores.
                GRBVar[,] X = new GRBVar[nVertices, nCores];
                var obj = new GRBLinExpr();

                for (int i = 0; i < nVertices; i++)
                {
                    for (int j = 0; j < nCores; j++)
                    {
                        string nome = "X_ " + i.ToString() + "_c " + j.ToString();

                        X[i, j] = model.AddVar(0.0, 1.0, 1.0, GRB.CONTINUOUS, nome);

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
                    model.AddConstr(soma, GRB.GREATER_EQUAL, 0, "Cor vértice" + i);
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

				bool solucaoInteira = true;
				for (int i = 0; i < nVertices; ++i)
				{
					for (int j = 0; j < nCores; ++j)
					{
						if ((X[i, j].Get(GRB.DoubleAttr.X) < (1 - epsilon)) && (X[i, j].Get(GRB.DoubleAttr.X) > (0 + epsilon)))
						{
							solucaoInteira = false;
						}
					}
				}

				int limiteSuperior = Heuristica(caminhoColorido, nCores);

				if (solucaoInteira == false)
				{
					Console.WriteLine("Solucao inteira nao encontrada");
					//BranchAndBound(model, nCores);
				}
				else
				{
					Console.WriteLine("Solucao inteira encontrada");
				}

				stopwatch.Stop();

				//MostrarMatriz(model, X, nVertices, nCores);

				/*
                Console.WriteLine("----------------------------------------------------------------------------------------");
                Console.WriteLine($"Número de variáveis = {model.NumVars}");
                Console.WriteLine($"Número de restrições lineares = {model.NumConstrs}");
                Console.WriteLine($"Número de coeficientes diferentes de zero na matriz de restrição = {model.NumNZs}");
                Console.WriteLine($"Valor objetivo para a solução atual: {model.ObjVal}");
                Console.WriteLine($"Tempo de execução para a otimização mais recente: {model.Runtime}");
                Console.WriteLine($"Número de nós de ramificação e corte explorados na otimização mais recente: {model.NodeCount}");
                Console.WriteLine($"Tempo total: {stopwatch.Elapsed}");
                Console.WriteLine("----------------------------------------------------------------------------------------");
				*/
				model.Dispose();
                env.Dispose();
            }
            catch (GRBException e)
            {
                Console.WriteLine("Código do erro: " + e.ErrorCode + ". " + e.Message + " - " + DateTime.Now);
                throw;
            }
        }

		public static void MostrarMatriz(GRBModel model, GRBVar[,] X, int nVertices, int nCores)
		{
			// Exibir resultado dos vértices participantes da solução
			double[,] x = model.Get(GRB.DoubleAttr.X, X);

			for (int i = 0; i < nVertices; i++)
			{
				for (int j = 0; j < nCores; j++)
				{
					Console.Write(x[i, j] + ";");
				}
				Console.WriteLine();
			}
		}

		public static int Heuristica(List<int> caminhoColorido, int nCores)
        {
            List<int> contCores = new List<int>();

            for (int i = 0; i < nCores; i++)
            {
                contCores.Add(0);
            }

            foreach (var item in caminhoColorido)
            {
                contCores[item - 1]++;
            }

            int corMaior = 0;
            int contMaior = 0;

            for (int i = 0; i < contCores.Count; i++)
            {
                if (contMaior < contCores[i])
                {
                    contMaior = contCores[i];
                    corMaior = i;
                }
                else if (contMaior == contCores[i])
                {
                    if (caminhoColorido[0] == corMaior + 1 && caminhoColorido[caminhoColorido.Count - 1] == corMaior + 1)
                    {
                        contMaior = contCores[i];
                        corMaior = i;
                    }
                    else if (caminhoColorido[0] == corMaior + 1 || caminhoColorido[caminhoColorido.Count - 1] == corMaior + 1)
                    {
                        if (caminhoColorido[0] == i + 1 || caminhoColorido[caminhoColorido.Count - 1] == i + 1)
                        {
                            continue;
                        }
                        else
                        {
                            contMaior = contCores[i];
                            corMaior = i;
                        }
                    }
                }
            }

            int naoTroca = 0;
            int corIni = caminhoColorido[0];
            int corFim = caminhoColorido[caminhoColorido.Count - 1];

            foreach (var item in caminhoColorido)
            {
                if ((item - 1) == corMaior || item != corIni)
                    break;
                else
                    naoTroca++;
            }

            for (int i = caminhoColorido.Count - 1; i >= 0; i--)
            {
                if ((caminhoColorido[i] - 1) == corMaior || caminhoColorido[i] != corFim || corIni == corFim)
                    break;
                else
                    naoTroca++;
            }

            return (caminhoColorido.Count - contMaior - naoTroca);
        }

		public class BB_Node
        {
			public int varToFix;
			public bool fix1;
			public BB_Node parent;
			public BB_Node left_child;
			public BB_Node right_child;
			public float UB;

			public BB_Node(int varToFix, bool fix1, BB_Node parent, float UB)
            {
				this.varToFix = varToFix;
				this.fix1 = fix1;
				this.parent = parent;
				this.left_child = null;
				this.right_child = null;
				this.UB = UB;
            }
        }

		public static BB_Node create_BB_node(int varToFix, bool fix1, BB_Node parent, float UB)
        {
			n_BB_nodes += 1;
			BB_Node no = new BB_Node(varToFix, fix1, parent, UB);
			return no;
        }

		public static BB_Node select_BB_node(List<BB_Node> L)
		{
			return L.Last();
		}

		public static void clear_branch_contraints()
		{
			/*
            for (int i = 0; i < nVertices; i++)
            {
                for (int j = 0; j < nCores; j++)
                {

                }
            }
			*/
		}

		public static void add_branch_contraints(BB_Node n_i, GRBModel model)
		{
            if (n_i.parent != null)
            {
                if (n_i.fix1)
                {
					GRBVar[] vars = model.GetVars();
					GRBVar vs = vars[n_i.varToFix];
					vs.Set(GRB.DoubleAttr.LB, 1);
				}
                else
                {
					GRBVar[] vars = model.GetVars();
					GRBVar vs = vars[n_i.varToFix];
					vs.Set(GRB.DoubleAttr.UB, 0);
				}

				add_branch_contraints(n_i.parent, model);
            }
		}

		public static void BranchAndBound2(GRBModel modelX, int heuristica)
		{
			int best_sol = heuristica;
			int z_ = heuristica;

			BB_Node n_0 = create_BB_node(-1, false, null, int.MaxValue);
			List<BB_Node> L = new List<BB_Node>();
			L.Add(n_0);
			var is_root_node = true;
			BB_Node root_node = null;

            while (L.Count != 0)
            {
				//Add Time Limit

				BB_Node n_i = select_BB_node(L);
				clear_branch_contraints();
				//add_branch_contraints(n_i, modeloX);
            }
		}

		/*
		public static double LBd;
		public static double UBd;

		public static void BranchAndBound(GRBModel modelX, int maiorValor) {
			int n_added_cuts_curr_iter = 0;
			List<GRBModel> models = new List<GRBModel>();
			List<int> indices = new List<int>();

			try
			{
				GRBVar[] vars = modelX.GetVars();

				GRBModel fixoA = new GRBModel(modelX);
				GRBModel fixoB = new GRBModel(modelX);


				//extrai as variaveis dos novos modelos 
				GRBVar[] varsA = fixoA.GetVars();
				GRBVar[] varsB = fixoB.GetVars();

				GRBVar vA = varsA[0];
				GRBVar vB = varsB[0];

				indices.Add(0);
				indices.Add(0);


				vA.Set(GRB.DoubleAttr.UB, 0);
				vB.Set(GRB.DoubleAttr.LB, 1);

				models.Add(fixoA);
				models.Add(fixoB);

				int j = 0;

				while ((indices.Count != 0) && (j < vars.Length - 1) && ((LBd + epsilon) < Math.Round(UBd)))
				{


					models[0].Optimize();

					bool solInteira0 = true;

					int optimstatus0 = models[0].Get(GRB.IntAttr.Status);

					if (optimstatus0 != GRB.Status.INFEASIBLE)
					{

						//verifica se a sol e inteira

						GRBVar[] var = models[0].GetVars();

						int t = 0;
						while (t < varsA.Length)
						{
							GRBVar v = var[t];
							if ((v.Get(GRB.DoubleAttr.X) < (1 - epsilon)) && (v.Get(GRB.DoubleAttr.X) > (0 + epsilon)))
							{
								solInteira0 = false;
								t = var.Length;
							}
							t = t + 1;
						}
					}

					if ((optimstatus0 == GRB.Status.INFEASIBLE) || (models[0].Get(GRB.DoubleAttr.ObjVal) < (LBd)))
					{

						models.RemoveAt(0);
						indices.RemoveAt(0);

					}
					else if ((solInteira0 == true) && (models[0].Get(GRB.DoubleAttr.ObjVal) >= LBd))
					{


						LBd = models[0].Get(GRB.DoubleAttr.ObjVal);

						j = indices[0] + 1;
						indices.Add(j);
						indices.Add(j);
						indices.RemoveAt(0);


						Decimal aproximador = new Decimal(0.00);
						int l = 0;
						GRBVar[] model0 = models[0].GetVars();
						for (int k = 0; k < model0.Length; k++)
						{

							GRBVar m0 = model0[k];

							//Console.WriteLine(m0.Get(GRB.DoubleAttr.X));
							l = l + 1;
							if (l == maiorValor)
							{
								l = 0;
								Console.WriteLine();
							}
						}

						GRBModel ModelA = new GRBModel(models[0]);
						GRBModel ModelB = new GRBModel(models[0]);

						GRBVar[] varsMA = ModelA.GetVars();
						GRBVar[] varsMB = ModelB.GetVars();

						GRBVar vsMA = varsMA[j];
						GRBVar vsMB = varsMB[j];

						vsMA.Set(GRB.DoubleAttr.UB, 0);
						vsMB.Set(GRB.DoubleAttr.LB, 1);

						models.Add(ModelA);
						models.Add(ModelB);

						models.RemoveAt(0);

					}
					else if ((solInteira0 == false) && (models[0].Get(GRB.DoubleAttr.ObjVal) >= LBd))
					{

						j = indices[0] + 1;
						indices.Add(j);
						indices.Add(j);
						indices.RemoveAt(0);

						GRBModel ModelA = new GRBModel(models[0]);
						GRBModel ModelB = new GRBModel(models[0]);

						GRBVar[] varsMA = ModelA.GetVars();
						GRBVar[] varsMB = ModelB.GetVars();

						GRBVar vsMA = varsMA[j];
						GRBVar vsMB = varsMB[j];

						vsMA.Set(GRB.DoubleAttr.UB, 0);
						vsMB.Set(GRB.DoubleAttr.LB, 1);

						models.Add(ModelA);
						models.Add(ModelB);

						models.RemoveAt(0);

					}
				}

				if (j == vars.Length - 1)
				{

					while (models.Count != 0)
					{

						models[0].Optimize();
						int Status = models[0].Get(GRB.IntAttr.Status);


						if ((Status == GRB.Status.INFEASIBLE) || (models[0].Get(GRB.DoubleAttr.ObjVal) < LBd))
						{

							models.RemoveAt(0);
							indices.RemoveAt(0);

						}
						else if (models[0].Get(GRB.DoubleAttr.ObjVal) >= LBd)
						{

							LBd = models[0].Get(GRB.DoubleAttr.ObjVal);




							Decimal aproximador = new Decimal(0.00);
							int l = 0;
							GRBVar[] model0 = models[0].GetVars();
							for (int k = 0; k < model0.Length; k++)
							{

								GRBVar m0 = model0[k];

								//Console.WriteLine(m0.Get(GRB.DoubleAttr.X));
								l = l + 1;
								if (l == maiorValor)
								{
									l = 0;
									Console.WriteLine();
								}
							}

							models.RemoveAt(0);
							indices.RemoveAt(0);

						}

					}
				}
				else
				{
					Decimal aproximador = new Decimal(0.00);
					int l = 0;
					GRBVar[] mod = models[models.Count - 1].GetVars();
					for (int k = 0; k < mod.Length; k++)
					{

						GRBVar m = mod[k];

						//Console.WriteLine(m.Get(GRB.DoubleAttr.X));
						l = l + 1;
						if (l == maiorValor)
						{
							l = 0;
							Console.WriteLine();
							models.Clear();
							indices.Clear();
						}
					}
				}
			}
			catch (GRBException e)
			{
				Console.WriteLine("Código do erro: " + e.ErrorCode + ". " + e.Message + " - " + DateTime.Now);
				throw;
			}
		}
		*/
    }
}
