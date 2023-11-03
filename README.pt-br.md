OmniNet - Networking em Nível Subatômico no Unity.
_______________________________________________________________________________

# OmniNet - Um Framework Unity para Desenvolvimento de Jogos Multiplayer

A OmniNet é um framework de Unity criado com o objetivo de proporcionar um alto desempenho no desenvolvimento de jogos multiplayer.

**Requisitos**: O OmniNet requer a versão 2021.2 ou superior do Unity. Isso se deve ao fato de que, a partir da versão 2021.2, o Unity adicionou suporte para o .Net Standard 2.1, trazendo melhorias significativas para o Network Socket, além de outras novidades como Span, Memory, ArrayPool, entre outras.

Decidi aproveitar essas novas funcionalidades para criar um framework de networking de alto desempenho.

## Pré-requisitos

Antes de usar este projeto, certifique-se de ter a seguinte dependência instalada:

- [com.unity.nuget.newtonsoft-json](https://github.com/jilleJr/Newtonsoft.Json-for-Unity)

Este pacote é necessário para lidar com a serialização e deserialização de JSON no projeto OmniNet. No entanto, é importante ressaltar que o OmniNet utiliza serialização binária para alcançar alta performance em suas operações.

Você pode instalá-lo através do Unity Package Manager seguindo estes passos:

1. Abra o Unity Editor.
2. Vá para **Window > Package Manager**.
3. Clique no botão **+** no canto superior esquerdo da janela do Package Manager.
4. Selecione **Add package by name...**.
5. Insira `com.unity.nuget.newtonsoft-json` como o nome do pacote.
6. Clique em **Add**.

No entanto, para obter um desempenho otimizado com o OmniNet, ele utiliza serialização binária em vez de JSON para suas operações principais.

## Documentação

[Documentação](../../wiki) - Aqui está a documentação completa do projeto.
