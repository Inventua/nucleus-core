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
	/// <internal>This class is an internal-use class, used by the setup wizard.</internal>
	/// <hidden/>
	public interface IPreflight
	{
		/// <summary>
		/// Result status type used by the <see cref="IPreflight"/> interface.
		/// </summary>
		/// <internal/>
		/// <hidden/>
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
		/// Class used by the <see cref="IPreflight"/> interface to represent pre-flight validation results.
		/// </summary>
		/// <internal/>
		/// <hidden/>
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

			/// <summary>
			/// Returns a count of errors.
			/// </summary>
			/// <returns></returns>
			public int ErrorCount()
			{
				return this.Where(result => result.Status == Status.Error).Count();
			}
		}

		/// <summary>
		/// Class used by the <see cref="IPreflight"/> interface to represent the result of each pre-flight validation step.
		/// </summary>
		/// <internal/>
		/// <hidden/>
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
			/// <param name="code"></param>
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
