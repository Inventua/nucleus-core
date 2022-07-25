# CSS Reference

## Admin

Admin CSS classes in `Resources/css/admin.css` can be used by ==Settings== views and ==Control Panel Extensions==.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| .nucleus-button-panel            | Class used to wrap buttons at the bottom of a form.  Buttons are rendered side-by-side with a small gap between them and a small top and bottom margin. | 
| .flex-fill                       | Specifies that a control should fill the available width.  Controls within a [`<SettingsControl>`](https://www.nucleus-cms.com/developers/razor-views/#settingscontrol-tag-helper) already fill the available width by default.  | 
| .nucleus-index-wrapper           | This class is intended to be used on a `div` which wraps a list of entries and an editor panel, which are rendered side-by-side. | 
| .nucleus-index-items             | This class is intended to be used on a `div` which is a child of .nucleus-index-wrapper, and which wraps an `ul` element.  It sets styles on the list and list items so that "items lists" are presented consistently. | 
| .nucleus-editor-panel            | When applied to a `div` which wraps an editor, applies flex styles to the panel (`div`), and any child forms or fieldsets so that they fill the available space. | 

## Shared
CSS classes in `Resources/css/shared.css` are always available.  CSS Classes which are used by Nucleus but are not intended for use 
by extensions are not included here.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| .fs-small                        | Render text at 0.9rem. | 
| .fs-smaller                      | Render text at 0.8rem. | 
| .btn.btn-none                    | Render a plain button control with no background, padding, border or margin. | 
| .nucleus-material-icon           | Use the Google Material Icons font. | 
| .ValidationError                 | The ValidationError class is automatically applied when a controller action returns a BadRequest(ControllerContext.ModelState) response.  It highlights controls with invalid content. | 
| .modal-auto-size.modal-dialog    | When applied to a bootstrap modal (.modal-dialog), the .modal-auto-size class overrides Bootstrap's modal width constraints and renders a modal that uses 80% of available screen width.  It also stretches content elements so that button rows are displayed consistently at the bottom of the dialog | 
| .w-min-fit-content               | The w-min-fit-content class is intended for table cells.  It applies width: 1px; white-space: nowrap; which causes the table cell to use the minimum width possible but still fit content without wrapping. | 
| .progress .indeterminate         | When applied to a Bootstrap progress bar ().progress) the .indeterminate class renders the progress as an indeterminate progress bar, so you can use a progress indicator when you don't know how long the operation will take. | 
| .nucleus-small-cell              | When applied to a table cell, renders the cell with width: 20px.  Use this class for table cells which contain a single small button. | 
| .ToggleSwitch                    | Apply the .ToggleSwitch class to an input[type=range] control to automatically enable the ToggleSwitch plugin. | 

## Forms

Forms CSS classes can be used by any part of Nucleus, including extensions, but ==Viewer== views must include forms.css by 
adding a call to `@Html.AddStyle("~/Resources/css/forms.css")` as forms.css is only automatically included in admin pages.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| .nucleus-flex-fields             | When applied to a `<div>` or another wrapper element, child elements are rendered in the same row (side-by-side). | 
| .settings-control                | This class is used to style elements which are rendered by the [`<SettingsControl>`](https://www.nucleus-cms.com/developers/razor-views/#settingscontrol-tag-helper) tag helper.  It applies styles to present input, select and other controls consistently. | 
| .nucleus-form-tools              | Class used to wrap buttons in the body of a form.  Buttons are rendered side-by-side with a small gap between them. | 
| .nucleus-form-buttonrow          | Class used to wrap buttons at the bottom of a form.  Buttons are rendered side-by-side with a small gap between them and a small top margin. | 
