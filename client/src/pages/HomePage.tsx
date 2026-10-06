import { Link } from 'react-router'

const previewColumns = [
  { name: 'Yapılacak', cards: ['Giriş sayfası tasarımı', 'API dokümantasyonu'] },
  { name: 'Yapılıyor', cards: ['Kart sürükle-bırak'] },
  { name: 'Bitti', cards: ['Veritabanı şeması', 'JWT ile giriş'] },
]

const features = [
  { title: 'Panolar ve ekipler', text: 'Pano oluştur, ekip arkadaşlarını e-posta ile davet et.' },
  { title: 'Sürükle-bırak', text: 'Kartları sütunlar arasında taşı, sırayı istediğin gibi düzenle.' },
  { title: 'Detaylı kartlar', text: 'Kişi ata, son tarih ve öncelik belirle, yorumlaş.' },
]

export function HomePage() {
  return (
    <div className="mx-auto max-w-6xl px-4">
      <section className="py-16 text-center sm:py-24">
        <h1 className="text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
          Ekibinle işlerini <span className="text-indigo-600">tek panoda</span> yönet
        </h1>
        <p className="mx-auto mt-4 max-w-xl text-lg text-slate-600">
          Görevleri sütunlara ayır, kartları sürükle, kimin neyle uğraştığını anında gör.
        </p>
        <div className="mt-8 flex justify-center gap-3">
          <Link
            to="/register"
            className="rounded-md bg-indigo-600 px-5 py-2.5 font-medium text-white hover:bg-indigo-700"
          >
            Ücretsiz başla
          </Link>
          <Link
            to="/login"
            className="rounded-md border border-slate-300 bg-white px-5 py-2.5 font-medium text-slate-700 hover:bg-slate-50"
          >
            Giriş yap
          </Link>
        </div>
      </section>

      {/* Örnek pano önizlemesi: gerçek pano ekranı 9. adımda gelecek. */}
      <section className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid gap-4 sm:grid-cols-3">
          {previewColumns.map((column) => (
            <div key={column.name} className="rounded-lg bg-slate-100 p-3">
              <h3 className="mb-3 text-sm font-semibold text-slate-700">
                {column.name} <span className="font-normal text-slate-400">{column.cards.length}</span>
              </h3>
              <div className="space-y-2">
                {column.cards.map((card) => (
                  <div key={card} className="rounded-md bg-white p-3 text-sm shadow-sm">
                    {card}
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className="grid gap-6 py-16 sm:grid-cols-3">
        {features.map((feature) => (
          <div key={feature.title}>
            <h3 className="font-semibold text-slate-900">{feature.title}</h3>
            <p className="mt-1 text-sm text-slate-600">{feature.text}</p>
          </div>
        ))}
      </section>
    </div>
  )
}
