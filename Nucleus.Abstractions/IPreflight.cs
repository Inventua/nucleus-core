using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
	/// <summary>
	/// Class used to execute validation steps for the Nucleus environment.
	/// </summary>
	public interface IPreflight
	{

		/// <summary>
		/// Result status type
		/// </summary>
		public enum Status
		{
			/// <summary>
			/// The validation step succeeded.
			/// </summary>
			OK,
			/// <summary>
			/// A non-fatal condition was detected.
			/// </summary>
			Warning,
			/// <summary>
			/// A fatal condition was detected.
			/// </summary>
			Error
		}

		/// <summary>
		/// Validate the environment and return validation results.
		/// </summary>
		/// <returns></returns>
		public ValidationResults Validate();

		/// <summary>
		/// Class used to represent pre-flight validation results 
		/// </summary>
		public class ValidationResults : List<ValidationResult>
		{
			/// <summary>
			/// Returns a value specifying whether any results have an error state.
			/// </summary>
			/// <returns></returns>
			public Boolean IsValid()
			{
				return !this.Where(result => result.Status == Status.Error).Any();
			}
		}


		/// <summary>
		/// Class used to represent the result of each pre-flight validation step.
		/// </summary>
		public class ValidationResult
		{
			/// <summary>
			/// Result code.
			/// </summary>
			public string Code { get; }

			/// <summary>
			/// Result status.
			/// </summary>
			public Status Status { get; }

			/// <summary>
			/// Result message.
			/// </summary>
			public string Message { get; }

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="status"></param>
			/// <param name="message"></param>
			public ValidationResult(string code, Status status, string message)
			{
				this.Code = code;
				this.Status = status;
				this.Message = message;
			}
		}
	}
}
