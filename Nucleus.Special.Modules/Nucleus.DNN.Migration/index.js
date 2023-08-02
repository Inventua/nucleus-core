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

  jQuery(document).on('change', '.folder-selection-checkbox', function (event)
  {
    var selectedItem = jQuery(this);
    var selectedItemId = selectedItem.attr('data-folderid');
    
    if (selectedItem.is(':checked'))
    {
      // when a folder is checked, select all children, but only if they are currently all un-selected
      if (jQuery('.folder-selection-checkbox[data-parentid="' + selectedItemId + '"]:checked').length === 0)
      {
        jQuery('.folder-selection-checkbox[data-parentid="' + selectedItemId + '"]').prop('checked', true);
      }
    }
    else
    {
      // when a folder is un-checked, un-select all children
      jQuery('.folder-selection-checkbox[data-parentid="' + selectedItemId + '"]').prop('checked', false);
    }
    
  });

});