// Update Name input when the user types in Code input
function updateNameFromInput(inputElement, index) {
    debugger
    const code = inputElement.value.trim();
    const nameInput = inputElement.closest('tr').querySelector('.itemName');

    if (!code) {
        nameInput.value = '';
        clearDatalistOptions('itemOptions' + index);
        return;
    }

    // Fetch matching items from controller
    const warehouseId = document.getElementById('WarehouseId').value || 0;

    fetch(`/ItemStockSettlement/SearchByCode?term=${encodeURIComponent(code)}&warehouseId=${warehouseId}`)
        .then(res => res.json())
        .then(data => {
            // Populate datalist
            const datalist = document.getElementById('itemOptions' + index);
            datalist.innerHTML = '';

            data.forEach(item => {
                const option = document.createElement('option');
                option.value = item.code; // user selects this code
                option.textContent = item.code + ' - ' + item.name;
                datalist.appendChild(option);
            });

            // Auto-fill name if exact match exists
            const exactMatch = data.find(i => i.code.toLowerCase() === code.toLowerCase());
            nameInput.value = exactMatch ? exactMatch.name : '';
        })
        .catch(err => console.error(err));
}

// Clear datalist options
function clearDatalistOptions(datalistId) {
    const datalist = document.getElementById(datalistId);
    if (datalist) datalist.innerHTML = '';
}

// Initialize datalist for first row
document.addEventListener('DOMContentLoaded', function () {
    const inputElement = document.getElementById('itemInput0');
    if (inputElement.value.trim() !== '') {
        updateNameFromInput(inputElement, 0);
    }
});


  
//// Update Name input when the user types in Code input
//function updateNameFromInput(inputElement) {
//    const row = inputElement.closest('tr');      // get the parent row
//    const table = row.closest('table');          // get the table
//    const index = row.dataset.index || row.rowIndex - 1; // use dataset index or fallback to rowIndex

//    const code = inputElement.value.trim();
//    const nameInput = row.querySelector('.itemName');
//    const datalistId = `itemOptions${index}`;
   
//    if (!code) {
//        nameInput.value = '';
//        const datalist = document.getElementById(datalistId);
//        if (datalist) datalist.innerHTML = '';
//        return;
//    }

//    const warehouseId = document.getElementById('WarehouseId').value || 0;
//    debugger
//    fetch(`/ItemStockSettlement/GetItems?term=${encodeURIComponent(code)}&warehouseId=${warehouseId}`)
//        .then(res => res.json())
//        .then(data => {
//            const datalist = document.getElementById(datalistId);
//            datalist.innerHTML = '';

//            data.forEach(item => {
//                const option = document.createElement('option');
//                option.value = item.code;
//                option.textContent = `${item.code} - ${item.name}`;
//                datalist.appendChild(option);
//            });

//            const exactMatch = data.find(i => i.code.toLowerCase() === code.toLowerCase());
//            nameInput.value = exactMatch ? exactMatch.name : '';
//        })
//        .catch(err => console.error(err));
//}

//// Clear datalist options
//function clearDatalistOptions(datalistId) {
//    const datalist = document.getElementById(datalistId);
//    if (datalist) datalist.innerHTML = '';
//}

//// Add a new row dynamically
//function addRow() {
//    const table = document.getElementById('itemsTable').querySelector('tbody');
//    const index = rowIndex++;

//    const row = document.createElement('tr');
//    row.dataset.index = index; // store index for datalist
//    row.innerHTML = `
//        <td>${index + 1}</td>
//        <td>
//            <input type="text" 
//                   id="itemInput${index}"
//                   name="Items[${index}].Code" 
//                   class="form-control itemCode" 
//                   list="itemOptions${index}" 
//                   oninput="updateNameFromInput(this)" />
//            <datalist id="itemOptions${index}"></datalist>
//        </td>
//        <td>
//         <input type="text"
//                   id="itemInput${index}"
//                   name="Items[${index}].Name" 
//                   class="form-control itemName" 
//                   list="itemOptions${index}" 
//                   oninput="updateNameFromInput(this)" />
//            <datalist id="itemOptions${index}"></datalist>
                       
//        </td>
//        <td>
//            <input type="text" name="Items[${index}].NewOnHand" class="form-control" />
//        </td>
//        <td class="text-center">
//            <a class="btn btn-danger" onclick="removeRow(this)">REMOVE</a>
//        </td>
//    `;
//    table.appendChild(row);
//}

//// Remove row
//function removeRow(button) {
//    button.closest('tr').remove();
//}

//// Initialize first row if needed
//document.addEventListener('DOMContentLoaded', function () {
//    const inputElement = document.getElementById('itemInput0');
//    if (inputElement && inputElement.value.trim() !== '') {
//        updateNameFromInput(inputElement);
//    }
//});
