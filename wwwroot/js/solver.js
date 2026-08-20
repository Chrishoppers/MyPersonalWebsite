(function(){
    const fileInput = document.getElementById('fileInput');
    const btnUpload = document.getElementById('btnUpload');
    const btnCamera = document.getElementById('btnCamera');
    const previewImage = document.getElementById('previewImage');
    const video = document.getElementById('video');
    const btnCrop = document.getElementById('btnCrop');
    const statusEl = document.getElementById('status');
    const problemsEl = document.getElementById('problems');
    let cropper = null;
    let stream = null;
    let connectionId = null;

    btnUpload.addEventListener('click', ()=> fileInput.click());
    fileInput.addEventListener('change', async (e)=>{
        const f = e.target.files[0];
        if (!f) return;
        const url = URL.createObjectURL(f);
        showImage(url);
        // keep file for upload
        previewImage._file = f;
    });

    btnCamera.addEventListener('click', async ()=>{
        if (video.style.display === 'none'){
            try{
                stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false });
                video.srcObject = stream;
                video.style.display = 'block';
                previewImage.style.display = 'none';
            }catch(e){ alert('摄像头打开失败：' + e); }
        } else {
            // take photo
            const canvas = document.createElement('canvas');
            canvas.width = video.videoWidth; canvas.height = video.videoHeight;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(video,0,0,canvas.width,canvas.height);
            const dataUrl = canvas.toDataURL('image/png');
            showImage(dataUrl);
            // stop stream
            stream.getTracks().forEach(t=>t.stop());
            video.style.display = 'none';
        }
    });

    function showImage(url){
        previewImage.src = url;
        previewImage.style.display = 'block';
        if (cropper) { cropper.destroy(); cropper = null; }
        cropper = new Cropper(previewImage, { viewMode:1, autoCropArea:0.8 });
    }

    btnCrop.addEventListener('click', async ()=>{
        if (!cropper) { alert('请先上传或拍照图片'); return; }
        statusEl.textContent = '正在导出裁剪图片...';
        const canvas = cropper.getCroppedCanvas({ maxWidth:1600, maxHeight:1600, imageSmoothingQuality:'high' });
        const blob = await new Promise(res=> canvas.toBlob(res,'image/png'));
        const fd = new FormData();
        fd.append('image', blob, 'capture.png');
        statusEl.textContent = '上传图片并开始识别...';
        try{
            const resp = await fetch('/Solver/Upload', { method:'POST', body: fd });
            const data = await resp.json();
            if (!data.success){ statusEl.textContent = '识别失败：' + (data.message||''); return; }
            statusEl.textContent = '识别完成，渲染题目...';
            renderProblems(data.problems);
        }catch(e){ statusEl.textContent = '上传失败：' + e; }
    });

    function renderProblems(list){
        problemsEl.innerHTML = '';
        if (!list || list.length==0){ problemsEl.innerHTML = '<div style="color:rgba(255,255,255,0.6)">未检测到题目</div>'; return; }
        list.forEach((p, idx)=>{
            const div = document.createElement('div'); div.className='problem-card';
            div.innerHTML = `<div><strong>题 ${idx+1}</strong> ${p.shortText}</div>
                <div style="margin-top:6px"><button data-id="${p.id}" class="btn btn-ask">查看 AI 解答</button></div>
                <div class="ai-answer" id="answer_${p.id}"></div>`;
            problemsEl.appendChild(div);
        });
        // bind
        document.querySelectorAll('.btn-ask').forEach(b=> b.addEventListener('click', async (e)=>{
            const id = e.target.getAttribute('data-id');
            const card = list.find(x=>x.id===id);
            if (!card) return;
            const el = document.getElementById('answer_'+id);
            el.textContent = 'AI 正在作答...';
            try{
                const resp = await fetch('/Solver/Ask', { method:'POST', headers:{'Content-Type':'application/x-www-form-urlencoded'}, body: 'question='+encodeURIComponent(card.fullText) });
                const data = await resp.json();
                if (data.success){ el.textContent = data.answer; } else { el.textContent = 'AI 失败'; }
            }catch(e){ el.textContent = '请求失败: '+e; }
        }) );
    }

})();
