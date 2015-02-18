﻿using Rally.RestApi.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Rally.RestApi.Connection;

namespace Rally.RestApi.UiForWpf
{
	/// <summary>
	/// A WPF based authentication manager.
	/// </summary>
	public class RestApiAuthMgrWpf : ApiAuthManager
	{
		static Dictionary<CustomWpfControlType, Type> customControlTypes = new Dictionary<CustomWpfControlType, Type>();
		static ImageSource logoForUi = null;
		LoginWindow loginControl = null;
		internal SsoAuthenticationComplete LoginWindowSsoAuthenticationComplete;

		#region Constructor
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="applicationToken">An application token to be used as the file name to store data as. Each 
		/// consuming application should use a unique name in order to ensure that the user credentials are not 
		/// overwritten by other applications.</param>
		/// <param name="encryptionKey">The encryption key, or salt, to be used for any encryption routines. This salt 
		/// should be different for each user, and not the same for everyone consuming the same application. Only used 
		/// for UI support.</param>
		/// <param name="encryptionRoutines">The encryption routines to use for encryption/decryption of data. Only used for UI support.</param>
		/// <param name="webServiceVersion">The version of the WSAPI API to use.</param>
		/// <example>
		/// <code language="C#">
		/// // You must define your own private application token. This ensures that your login details are not overwritten by someone else.
		/// string applicationToken = "RallyRestAPISample";
		/// // You must set a user specific salt for encryption.
		/// string encryptionKey = "UserSpecificSaltForEncryption";
		/// // You must define your own encryption routines.
		/// IEncryptionRoutines encryptionUtilities = new EncryptionUtilities();
		/// 
		/// // Instantiate authorization manager
		/// wpfAuthMgr = new RestApiAuthMgrWpf(applicationToken, encryptionKey, encryptionUtilities);
		/// </code>
		/// </example>
		public RestApiAuthMgrWpf(string applicationToken, string encryptionKey,
			IEncryptionRoutines encryptionRoutines, string webServiceVersion = RallyRestApi.DEFAULT_WSAPI_VERSION)
			: base(true, applicationToken, encryptionKey, encryptionRoutines, webServiceVersion)
		{
			// NOTE: The example for this constructor is also used for the RallyRestApi
			// constructor. Make sure you update both if you change it.
		}
		#endregion

		#region SetLogo
		/// <summary>
		/// Sets the logo used in the user controls.
		/// </summary>
		/// <param name="logo">The image to use as a logo.</param>
		/// <example>
		/// <code language="C#">
		/// // ImageResources is a resource file that the logo has been added to.
		/// Bitmap bitMap = ImageResources.Logo;
		/// RestApiAuthMgrWpf.SetLogo(GetImageSource(bitMap));
		/// </code>
		/// This is a sample helper method for converting a Bitmap to an <see cref="System.Windows.Media.ImageSource"/>.
		/// <code language="C#">
		/// public static media.ImageSource GetImageSource(Bitmap image)
		/// {
		/// 	return Imaging.CreateBitmapSourceFromHBitmap(
		/// 										image.GetHbitmap(),
		/// 										IntPtr.Zero,
		/// 										Int32Rect.Empty,
		/// 										BitmapSizeOptions.FromEmptyOptions());
		/// }
		/// </code>
		/// </example>
		public static void SetLogo(ImageSource logo)
		{
			logoForUi = logo;
		}
		#endregion

		#region ShowUserLoginWindowInternal
		/// <summary>
		/// Opens the window that displays the SSO URL to the user.
		/// </summary>
		protected override void ShowUserLoginWindowInternal()
		{
			// If the login control exists, don't open a new one.
			if (loginControl == null)
			{
				loginControl = new LoginWindow();
				loginControl.BuildLayout(this);
				loginControl.Closed += loginControl_Closed;
				LoginWindowSsoAuthenticationComplete += loginControl.SsoAuthenticationComplete;
			}

			loginControl.SetLogo(logoForUi);
			loginControl.UpdateLoginState();
			loginControl.Show();
			loginControl.Focus();
		}
		#endregion

		#region OpenSsoPageInternal
		/// <summary>
		/// Opens the specified SSO URL to the user.
		/// </summary>
		/// <param name="ssoUrl">The Uri that the user was redirected to in order to perform their SSO authentication.</param>
		protected override void OpenSsoPageInternal(Uri ssoUrl)
		{
			try
			{
				SsoWindow window = new SsoWindow();
				window.ShowSsoPage(this, ssoUrl);
			}
			catch
			{
				SsoWindow window = null;
				Application.Current.Dispatcher.Invoke(delegate() { window = new SsoWindow(); });
				if (window != null)
				{
					Application.Current.Dispatcher.Invoke(delegate() { window.ShowSsoPage(this, ssoUrl); });
				}
			}
		}
		#endregion

		#region ReportSsoResults
		/// <summary>
		/// Reports the results of an SSO action.
		/// </summary>
		/// <param name="success">Was SSO authentication completed successfully?</param>
		/// <param name="rallyServer">The server that the ZSessionID is for.</param>
		/// <param name="zSessionID">The zSessionID that was returned from Rally.</param>
		internal void ReportSsoResultsToMgr(bool success, string rallyServer, string zSessionID)
		{
			ReportSsoResults(success, rallyServer, zSessionID);
		}
		#endregion

		#region NotifyLoginWindowSsoComplete
		/// <summary>
		/// Notifies the login window that SSO has been completed.
		/// </summary>
		/// <param name="authenticationResult">The current state of the authentication process. <see cref="RallyRestApi.AuthenticationResult"/></param>
		/// <param name="api">The API that was authenticated against.</param>
		protected override void NotifyLoginWindowSsoComplete(
			RallyRestApi.AuthenticationResult authenticationResult, RallyRestApi api)
		{
			if (LoginWindowSsoAuthenticationComplete != null)
				LoginWindowSsoAuthenticationComplete.Invoke(authenticationResult, api);
		}
		#endregion

		#region loginControl_Closed
		void loginControl_Closed(object sender, EventArgs e)
		{
			LoginWindowSsoAuthenticationComplete = null;
			loginControl = null;
		}
		#endregion

		#region PerformAuthenticationCheck
		/// <summary>
		/// Performs an authentication check against Rally with the specified credentials
		/// </summary>
		internal new RallyRestApi.AuthenticationResult PerformAuthenticationCheck(out string errorMessage)
		{
			return base.PerformAuthenticationCheck(out errorMessage);
		}
		#endregion

		#region PerformLogout
		/// <summary>
		/// Performs an logout from Rally.
		/// </summary>
		internal void PerformLogout()
		{
			base.PerformLogoutFromRally();
		}
		#endregion

		#region SetCustomControlType
		/// <summary>
		/// Sets a custom control for the specified control type. Please see the
		/// enumeration help for what the types must extend from in order to work.
		/// </summary>
		/// <param name="customWpfControlType">The control type that we want to use a custom control for.</param>
		/// <param name="type">The type of the control to use for the specified customWpfControlType.</param>
		/// <example>
		/// <code language="C#">
		/// RestApiAuthMgrWpf.SetCustomControlType(CustomWpfControlType.Buttons, typeof(CustomButton));
		/// </code>
		/// </example>
		public static void SetCustomControlType(CustomWpfControlType customWpfControlType, Type type)
		{
			if (customControlTypes.ContainsKey(customWpfControlType))
				customControlTypes[customWpfControlType] = type;
			else
				customControlTypes.Add(customWpfControlType, type);
		}
		#endregion

		#region GetCustomControlType
		/// <summary>
		/// Gets a custom control for the specified control type. 
		/// </summary>
		internal static Type GetCustomControlType(CustomWpfControlType customWpfControlType)
		{
			if (customControlTypes.ContainsKey(customWpfControlType))
				return customControlTypes[customWpfControlType];

			return null;
		}
		#endregion
	}
}
