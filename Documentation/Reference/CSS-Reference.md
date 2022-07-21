# CSS Reference

## Admin

Admin CSS classes can be used by ==Settings== views and ==Control Panel Extensions==.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
|                                  | . | 
|                                  | . | 
|                                  | . | 
|                                  | . | 
|                                  | . | 
|                                  | . | 
|                                  | . | 
|                                  | . | 
|                                  | . | 

## Shared

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
|                                  | . | 
|                                  | . | 


## Forms

Forms CSS classes can be used by any part of Nucleus, including extensions, but ==Viewer== views must include forms.css by 
adding a call to @Html.AddStyle("~/Resources/css/forms.css") as forms.css is only automatically included in admin pages.

|                                  |                                                                                      |
|----------------------------------|--------------------------------------------------------------------------------------|
| .nucleus-flex-fields             | When applied to a `<div>` or other wrapper element, child elements are rendered in the same row (side-by-side). | 
| .settings-control                | This class is used to style elements which are rendered by `<settingscontrol>` tag helper. | 
| .nucleus-form-tools              | Class used to wrap buttons in the body of a form.  Buttons are rendered side-by-side with a small gap between them. | 
| .nucleus-form-buttonrow          | Class used to wrap buttons at the bottom of a form.  Buttons are rendered side-by-side with a small gap between them and a small top margin. | 
