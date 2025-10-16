// Update Name input when the user types in Code input
function updateNameFromInput(inputElement, index) {
   
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
            debugger
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
