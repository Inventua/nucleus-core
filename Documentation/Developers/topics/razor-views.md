## Razor Views

@Html.AddStyle("~!/../settings.css")

<h1 class="nucleus-modal-caption">Documents Settings</h1>


Tab Tag Helper

fieldsets

@using (Html.BeginNucleusForm("Settings", "Documents", FormMethod.Post, new { @enctype = "multipart/form-data" }))

data-target


SettingsControl tag helper


					<div class="nucleus-form-tools">
						@Html.SubmitButton("", "Save Settings", @Url.NucleusAction("SaveSettings", "Documents", "Documents"), new { })
					</div>