﻿using System;
using System.Collections.Generic;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс обеспечивает вычисление выражения по цепочке шагов
	/// </summary>
	public class ExpressionEvaluator
		{
		/// <summary>
		/// Возвращает результат вычисления выражения
		/// </summary>
		public double Result
			{
			get
				{
				return result;
				}
			}
		private double result;

		/// <summary>
		/// Ограничение ординат при вычислении
		/// </summary>
		public const double EvaluationLimit = 1.0e+6;

		/// <summary>
		/// Конструктор. Вычисляет значение выражения, представленного в форме цепочки шагов
		/// </summary>
		/// <param name="Chain">Цепочка шагов вычисления</param>
		/// <param name="VariableValue">Значение переменной</param>
		public ExpressionEvaluator (EvaluationChainAssembler Chain, double VariableValue)
			{
			// Контроль
			if (!Chain.IsInited)
				return;

			// Массив промежуточных результатов
			List<double> semiresults = [];

			// Вычисление
			for (int i = 0; i < Chain.EvaluationChain.Count; i++)
				{
				// Определение операндов
				double operand1 = 0.0, operand2 = 0.0;
				switch (Chain.EvaluationChain[i].Operand1Type)
					{
					case EvaluationChainElement.OperandTypes.Link:
						operand1 = semiresults[(int)Chain.EvaluationChain[i].Operand1Value];
						break;

					case EvaluationChainElement.OperandTypes.Number:
						operand1 = Chain.EvaluationChain[i].Operand1Value;
						break;

					case EvaluationChainElement.OperandTypes.Variable:
						operand1 = VariableValue;
						break;
					}

				switch (Chain.EvaluationChain[i].Operand2Type)
					{
					case EvaluationChainElement.OperandTypes.Link:
						operand2 = semiresults[(int)Chain.EvaluationChain[i].Operand2Value];
						break;

					case EvaluationChainElement.OperandTypes.Number:
						operand2 = Chain.EvaluationChain[i].Operand2Value;
						break;

					case EvaluationChainElement.OperandTypes.Variable:
						operand2 = VariableValue;
						break;
					}

				// Вычисление
				switch (Chain.EvaluationChain[i].OperationType)
					{
					case EvaluationChainElement.OperationTypes.Abs:
						semiresults.Add (Math.Abs (operand2));
						break;

					case EvaluationChainElement.OperationTypes.Addition:
						semiresults.Add (operand1 + operand2);
						break;

					case EvaluationChainElement.OperationTypes.Arccosinus:
						semiresults.Add (Math.Acos (operand2));
						break;

					case EvaluationChainElement.OperationTypes.Arccotangens:
						semiresults.Add (Math.Atan (-operand2) + Math.PI / 2.0);
						break;

					case EvaluationChainElement.OperationTypes.Arcsinus:
						semiresults.Add (Math.Asin (operand2));
						break;

					case EvaluationChainElement.OperationTypes.Arctangens:
						semiresults.Add (Math.Acos (operand2));
						break;

					case EvaluationChainElement.OperationTypes.Cosinus:
						semiresults.Add (Math.Cos (operand2));
						break;

					case EvaluationChainElement.OperationTypes.Cotangens:
						semiresults.Add (1.0 / Math.Tan (operand2));
						break;

					case EvaluationChainElement.OperationTypes.Division:
						semiresults.Add (operand1 / operand2);
						break;

					case EvaluationChainElement.OperationTypes.Exponentiation:
						semiresults.Add (Math.Pow (operand1, operand2));
						break;

					case EvaluationChainElement.OperationTypes.Multiplication:
						semiresults.Add (operand1 * operand2);
						break;

					case EvaluationChainElement.OperationTypes.NaturalLogarithm:
						semiresults.Add (Math.Log (operand2));
						break;

					case EvaluationChainElement.OperationTypes.Nothing:
						semiresults.Add (operand2);
						break;

					case EvaluationChainElement.OperationTypes.Sinus:
						semiresults.Add (Math.Sin (operand2));
						break;

					case EvaluationChainElement.OperationTypes.Subtraction:
						semiresults.Add (operand1 - operand2);
						break;

					case EvaluationChainElement.OperationTypes.Tangens:
						semiresults.Add (Math.Tan (operand2));
						break;
					}
				}

			// Завершено. Возврат результата со стандартным усечением
			if (semiresults[semiresults.Count - 1] > EvaluationLimit)
				{
				result = EvaluationLimit;
				}
			else if (semiresults[semiresults.Count - 1] < -EvaluationLimit)
				{
				result = -EvaluationLimit;
				}
			else
				{
				result = semiresults[semiresults.Count - 1];
				}
			evaluationResult = EvaluationResults.Ok;
			}

		/// <summary>
		/// Возможные результаты вычисления выражения
		/// </summary>
		public enum EvaluationResults
			{
			/// <summary>
			/// Вычисление выполнено успешно
			/// </summary>
			Ok,

			/// <summary>
			/// Получена неинициализированная цепочка шагов
			/// </summary>
			ChainNotInited,

			/// <summary>
			/// На одном из шагов вычисления получена бесконечность или невычислимое значение
			/// </summary>
			EvaluationOverflowOrFailure
			}

		/// <summary>
		/// Возвращает описание результата вычисления выражения
		/// </summary>
		public EvaluationResults EvaluationResult
			{
			get
				{
				return evaluationResult;
				}
			}
		private EvaluationResults evaluationResult = EvaluationResults.ChainNotInited;
		}
	}
