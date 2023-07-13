jQuery(document).ready(function ()
{
  jQuery(document).on('change', '.toggle-all', function (event)
  {
    var dialog = jQuery(this).parents('table').first(); 
    var elements = dialog.find('input[type=checkbox]:not(.toggle-all)');
    var selectedCount = dialog.find('input[type=checkbox]:checked:not(.toggle-all)').length;
    var isChecked = (selectedCount === 0);
    elements.prop('checked', isChecked);
  });
});